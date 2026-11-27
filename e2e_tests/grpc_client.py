"""
Lightweight zero-dependency gRPC over HTTP/2 (h2c) client for ShuffleSeries E2E tests.
Uses system curl (with nghttp2 HTTP/2 prior-knowledge support) for framing/trailers
and pure Python struct/varint helpers for Protobuf wire encoding and decoding.
"""

import struct
import subprocess


def encode_varint(value: int) -> bytes:
    out = bytearray()
    v = value & 0xFFFFFFFFFFFFFFFF
    while True:
        byte = v & 0x7F
        v >>= 7
        if v:
            out.append(byte | 0x80)
        else:
            out.append(byte)
            break
    return bytes(out)


def encode_field_varint(field_num: int, value: int) -> bytes:
    tag = (field_num << 3) | 0
    return encode_varint(tag) + encode_varint(value)


def encode_field_bytes(field_num: int, data: bytes) -> bytes:
    tag = (field_num << 3) | 2
    return encode_varint(tag) + encode_varint(len(data)) + data


def encode_field_str(field_num: int, text: str) -> bytes:
    return encode_field_bytes(field_num, text.encode("utf-8"))


def decode_varint(data: bytes, offset: int) -> tuple[int, int]:
    result = 0
    shift = 0
    while offset < len(data):
        byte = data[offset]
        offset += 1
        result |= (byte & 0x7F) << shift
        if not (byte & 0x80):
            return result, offset
        shift += 7
    raise ValueError("Truncated varint in protobuf payload")


def decode_protobuf_fields(data: bytes) -> dict[int, list[tuple[int, object]]]:
    fields: dict[int, list[tuple[int, object]]] = {}
    offset = 0
    while offset < len(data):
        tag, offset = decode_varint(data, offset)
        field_num = tag >> 3
        wire_type = tag & 0x07

        if wire_type == 0:
            val, offset = decode_varint(data, offset)
        elif wire_type == 1:
            val = data[offset:offset + 8]
            offset += 8
        elif wire_type == 2:
            length, offset = decode_varint(data, offset)
            val = data[offset:offset + length]
            offset += length
        elif wire_type == 5:
            val = data[offset:offset + 4]
            offset += 4
        else:
            raise ValueError(f"Unsupported protobuf wire type: {wire_type}")

        fields.setdefault(field_num, []).append((wire_type, val))
    return fields


def get_first_str(fields: dict[int, list[tuple[int, object]]], field_num: int, default: str = "") -> str:
    entries = fields.get(field_num)
    if not entries:
        return default
    return entries[0][1].decode("utf-8")


def get_first_int(fields: dict[int, list[tuple[int, object]]], field_num: int, default: int = 0) -> int:
    entries = fields.get(field_num)
    if not entries:
        return default
    return int(entries[0][1])


def decode_string_map(entries: list[tuple[int, object]]) -> dict[str, dict[int, list[tuple[int, object]]]]:
    result: dict[str, dict[int, list[tuple[int, object]]]] = {}
    for _, raw in entries:
        entry_fields = decode_protobuf_fields(raw)
        key = get_first_str(entry_fields, 1)
        val_entries = entry_fields.get(2)
        val_fields = decode_protobuf_fields(val_entries[0][1]) if val_entries else {}
        result[key] = val_fields
    return result


def call_grpc(host: str, port: int, full_method: str, request_bytes: bytes) -> tuple[int, int, bytes, str]:
    """
    Executes a unary gRPC call over HTTP/2 cleartext (h2c prior knowledge).
    Returns: (http_status, grpc_status, response_protobuf_bytes, grpc_message)
    """
    grpc_frame = b"\x00" + struct.pack(">I", len(request_bytes)) + request_bytes
    url = f"http://{host}:{port}{full_method}"

    proc = subprocess.run(
        [
            "curl", "-sS", "-i",
            "--http2-prior-knowledge",
            "--max-time", "10",
            "-X", "POST",
            "-H", "Content-Type: application/grpc",
            "-H", "TE: trailers",
            "--data-binary", "@-",
            url,
        ],
        input=grpc_frame,
        capture_output=True,
    )

    if proc.returncode != 0:
        err = proc.stderr.decode("utf-8", errors="replace").strip()
        return 0, -1, b"", f"curl error ({proc.returncode}): {err}"

    raw = proc.stdout
    sep_idx = raw.find(b"\r\n\r\n")
    if sep_idx == -1:
        return 0, -1, b"", "Malformed HTTP/2 response without header separator"

    header_block = raw[:sep_idx].decode("latin-1")
    rest = raw[sep_idx + 4:]

    http_status = 0
    first_line = header_block.splitlines()[0] if header_block else ""
    parts = first_line.split(" ")
    if len(parts) >= 2 and parts[1].isdigit():
        http_status = int(parts[1])

    grpc_status = -1
    grpc_message = ""
    for line in header_block.splitlines()[1:]:
        if ":" in line:
            k, v = line.split(":", 1)
            kl = k.strip().lower()
            if kl == "grpc-status" and v.strip().lstrip("-").isdigit():
                grpc_status = int(v.strip())
            elif kl == "grpc-message":
                grpc_message = v.strip()

    # curl -i appends HTTP/2 trailing headers after body OR merges them; parse uncompressed (flag=0) gRPC frames strictly
    payload_bytes = b""
    if len(rest) >= 5 and rest[0] == 0:
        msg_len = struct.unpack(">I", rest[1:5])[0]
        frame_end = 5 + msg_len
        payload_bytes = rest[5:frame_end]
        trailing = rest[frame_end:].decode("latin-1", errors="ignore")
    elif len(rest) >= 5 and rest[0] == 1:
        return http_status, -1, b"", "Compressed gRPC response frames (gzip flag=1) are not supported by E2E helper"
    else:
        trailing = rest.decode("latin-1", errors="ignore")

    for line in trailing.splitlines():
        if ":" in line:
            k, v = line.split(":", 1)
            kl = k.strip().lower()
            if kl == "grpc-status" and v.strip().lstrip("-").isdigit():
                grpc_status = int(v.strip())
            elif kl == "grpc-message":
                grpc_message = v.strip()

    return http_status, grpc_status, payload_bytes, grpc_message
