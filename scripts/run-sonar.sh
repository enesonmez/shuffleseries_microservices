#!/usr/bin/env bash
# ==============================================================================
# ShuffleSeries.Microservices - Local SonarQube Analysis Runner
# ==============================================================================
# Bu script, docker-compose.sonarqube.yml dosyasını kullanarak yerel SonarQube
# sunucusunu ayağa kaldırır, testleri çalıştırarak kod kapsama (coverage) raporu
# üretir ve statik kod analizi sonuçlarını SonarQube dashboard'una gönderir.
# ==============================================================================

set -euo pipefail

SONAR_URL="http://localhost:9000"
PROJECT_KEY="ShuffleSeries"
PROJECT_NAME="ShuffleSeries Microservices Platform"
COMPOSE_FILE="docker-compose.sonarqube.yml"

echo "================================================================="
echo "  🚀 ShuffleSeries Yerel SonarQube ve Test Kapsama Analizi"
echo "================================================================="

# 1. Java Kontrolü (SonarScanner için zorunlu)
if ! command -v java >/dev/null 2>&1; then
    echo "❌ HATA: Java runtime bulunamadı. SonarScanner çalıştırmak için Java 17 veya 21 gereklidir."
    echo "👉 macOS için: 'brew install openjdk@21' çalıştırabilirsiniz."
    exit 1
fi

# 2. Docker Kontrolü
if ! docker info >/dev/null 2>&1; then
    echo "❌ HATA: Docker daemon çalışmıyor. Lütfen Docker Desktop'ı başlatın."
    exit 1
fi

# 3. SonarQube Konteynerini Başlat
echo "📦 1. SonarQube altyapısı kontrol ediliyor..."
if [ "$(docker compose -f "$COMPOSE_FILE" ps -q sonarqube 2>/dev/null)" = "" ]; then
    echo "   SonarQube konteynerleri başlatılıyor ($COMPOSE_FILE)..."
    docker compose -f "$COMPOSE_FILE" up -d
else
    echo "   SonarQube konteyneri zaten ayakta."
fi

# 4. SonarQube Sunucusunun Hazır Olmasını Bekle
echo "⏳ 2. SonarQube sunucusunun hazır olması bekleniyor ($SONAR_URL)..."
RETRIES=60
until curl -s -f "$SONAR_URL/api/system/status" 2>/dev/null | grep -q '"status":"UP"'; do
    RETRIES=$((RETRIES - 1))
    if [ "$RETRIES" -le 0 ]; then
        echo "❌ HATA: SonarQube sunucusu zaman aşımına uğradı."
        echo "Detaylı loglar için: docker compose -f $COMPOSE_FILE logs sonarqube"
        exit 1
    fi
    printf "."
    sleep 3
done
echo ""
echo "✅ SonarQube sunucusu hazır!"

# 5. dotnet-sonarscanner Aracı Kontrolü
if ! command -v dotnet-sonarscanner >/dev/null 2>&1; then
    echo "🔧 3. dotnet-sonarscanner global aracı kuruluyor..."
    dotnet tool install --global dotnet-sonarscanner || true
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# 6. SonarQube Token Belirleme
# Ortam değişkeninden oku, yoksa kullanıcıya sor veya default kullan
SONAR_TOKEN="${SONAR_TOKEN:-}"
if [ -z "$SONAR_TOKEN" ]; then
    echo ""
    echo "🔑 SonarQube Giriş Anahtarı (Token):"
    echo "   SonarQube arayüzünden (http://localhost:9000 -> My Account -> Security) ürettiğiniz token'ı girebilirsiniz."
    echo "   Eğer ilk kurulum ise 'admin' şifresiyle token üretebilirsiniz."
    read -r -p "   SonarQube Token (Boş bırakırsanız 'admin' kullanıcı/şifresi denenecek): " INPUT_TOKEN
    if [ -n "$INPUT_TOKEN" ]; then
        SONAR_TOKEN="$INPUT_TOKEN"
    fi
fi

# 7. Kod Stili ve Format Doğrulaması (Fail-Fast Gate)
echo ""
echo "🧹 4. Kod stili ve format kontrolü yapılıyor (dotnet format)..."
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet format --verify-no-changes

# 8. Eski Test Sonuçlarını Temizle
rm -rf ./TestResults

# 9. SonarScanner Başlat (Begin)
echo ""
echo "🛡️ 5. SonarScanner analizi başlatılıyor..."
SCANNER_AUTH_ARGS=()
if [ -n "$SONAR_TOKEN" ]; then
    SCANNER_AUTH_ARGS+=("/d:sonar.token=$SONAR_TOKEN")
else
    # Token verilmediğinde varsayılan kullanıcı kimliği
    SCANNER_AUTH_ARGS+=("/d:sonar.login=admin" "/d:sonar.password=admin")
fi

dotnet-sonarscanner begin \
    /k:"$PROJECT_KEY" \
    /n:"$PROJECT_NAME" \
    /d:sonar.host.url="$SONAR_URL" \
    "${SCANNER_AUTH_ARGS[@]}" \
    /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" \
    /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" \
    /d:sonar.exclusions="**/bin/**,**/obj/**,**/*.Tests/**,**/Migrations/**,**/docker/**"

# 10. Çözümü Derle
echo ""
echo "🔨 6. Çözüm derleniyor (Release)..."
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build -c Release

# 11. Testleri Çalıştır ve Code Coverage Topla
echo ""
echo "🧪 7. Testler çalıştırılıyor ve Code Coverage toplanıyor..."
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet test -c Release --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults \
    --logger "trx" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover,cobertura

# 12. SonarScanner Tamamla (End)
echo ""
echo "🏁 8. SonarScanner analizi tamamlanıyor ve rapor sunucuya gönderiliyor..."
END_AUTH_ARGS=()
if [ -n "$SONAR_TOKEN" ]; then
    END_AUTH_ARGS+=("/d:sonar.token=$SONAR_TOKEN")
else
    END_AUTH_ARGS+=("/d:sonar.login=admin" "/d:sonar.password=admin")
fi

dotnet-sonarscanner end "${END_AUTH_ARGS[@]}"

echo ""
echo "================================================================="
echo "  🎉 Analiz Başarıyla Tamamlandı!"
echo "  📊 Sonuçları görüntülemek için SonarQube Dashboard'u açın:"
echo "  👉 $SONAR_URL/dashboard?id=$PROJECT_KEY"
echo "================================================================="
