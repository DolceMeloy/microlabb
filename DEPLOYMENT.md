# Развёртывание и тестирование RtuItLab

## Архитектура

```
                         ┌─────────────────────┐
                         │    Клиент / curl     │
                         └──────────┬──────────┘
                                    │ :5000
                         ┌──────────▼──────────┐
                         │   API Gateway        │
                         │   (Ocelot, :5000)    │
                         └──┬──┬──┬────────────┘
            ┌───────────────┘  │  └───────────────┐
            │                  │                  │
   ┌────────▼──────┐  ┌────────▼──────┐  ┌────────▼──────┐
   │ Identity API  │  │ Purchases API │  │   Shops API   │
   │   (:7001)     │  │   (:7002)     │  │   (:7003)     │
   └───────┬───────┘  └───────┬───────┘  └───────┬───────┘
           │                  │                  │
           └──────────────────┼──────────────────┘
                              │
                    ┌─────────▼─────────┐
                    │     RabbitMQ       │
                    │  (MassTransit)     │
                    └─────────┬─────────┘
                              │
            ┌─────────────────┼─────────────────┐
            │                 │                 │
   ┌────────▼──────┐  ┌───────▼───────┐  ┌─────▼───────────┐
   │  SQL Server   │  │  SQL Server   │  │   Factories API  │
   │  (identity)   │  │  (purchases,  │  │   (:7004)        │
   │               │  │   shops,      │  │   (фоновый       │
   │               │  │   factories)  │  │    воркер)       │
   └───────────────┘  └───────────────┘  └─────────────────┘
```

## Требования к серверу

| Компонент | Минимум |
|-----------|---------|
| ОС | Debian 10+ / Ubuntu 20.04+ |
| CPU | 2 ядра |
| RAM | 4 GB (SQL Server требует минимум 2 GB) |
| Диск | 10 GB свободного места |
| Docker | 20.10+ |
| Docker Compose | 2.0+ (или docker-compose 1.29+) |

---

## 1. Подготовка сервера (Debian)

### Установка Docker

```bash
# Обновить пакеты
sudo apt-get update && sudo apt-get upgrade -y

# Установить зависимости
sudo apt-get install -y ca-certificates curl gnupg lsb-release git

# Добавить GPG ключ Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg | \
  sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Добавить репозиторий Docker
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/debian \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Установить Docker Engine и Docker Compose
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Добавить текущего пользователя в группу docker (без sudo)
sudo usermod -aG docker $USER
newgrp docker

# Проверить версии
docker --version
docker compose version
```

### Разрешить SQL Server использовать необходимые ресурсы

```bash
# SQL Server требует vm.max_map_count >= 262144
echo "vm.max_map_count=262144" | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

---

## 2. Клонирование и настройка

```bash
# Клонировать репозиторий
git clone https://github.com/DolceMeloy/microlabb.git
cd microlabb/src

# Создать файл с переменными окружения из шаблона
cp .env.example .env
```

### Редактирование .env

```bash
nano .env
```

```env
# Пароль для SQL Server SA (минимум 8 символов, буквы, цифры, спецсимвол)
SA_PASSWORD=МойПаролЬ_2024!

# Секрет для подписи JWT токенов (сгенерировать новый!)
JWT_SECRET=$(openssl rand -hex 32)

ASPNETCORE_ENVIRONMENT=Production
```

> **Важно:** Если изменяете `SA_PASSWORD`, пароль должен содержать:
> заглавные и строчные буквы + цифры + специальный символ (`!`, `@`, `#` и т.д.)

---

## 3. Запуск

```bash
# Находясь в папке src/
cd microlabb/src

# Собрать образы и запустить все сервисы
# Флаг -f явно указывает только основной compose-файл,
# чтобы docker-compose.override.yml (для локальной разработки) не применялся
docker compose -f docker-compose.yml up --build -d

# Следить за логами запуска
docker compose logs -f
```

### Ожидаемая последовательность запуска

1. **rabbit** и **db** — стартуют первыми
2. Проходят healthcheck (занимает **~30–60 секунд**)
3. **identityapi**, **purchasesapi**, **shopsapi**, **factoriesapi** — запускаются после готовности db и rabbit
4. **apigateway** — запускается последним

Первый запуск занимает **3–7 минут** (скачивание образов + сборка).

### Проверка статуса

```bash
# Статус всех контейнеров
docker compose ps

# Ожидаемый вывод (все healthy/running):
# NAME           STATUS          PORTS
# src-db-1       Up (healthy)    0.0.0.0:1433->1433/tcp
# src-rabbit-1   Up (healthy)    0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
# src-identityapi-1    Up        0.0.0.0:7001->7001/tcp
# src-purchasesapi-1   Up        0.0.0.0:7002->7002/tcp
# src-shopsapi-1       Up        0.0.0.0:7003->7003/tcp
# src-factoriesapi-1   Up        0.0.0.0:7004->7004/tcp
# src-apigateway-1     Up        0.0.0.0:5000->5000/tcp
```

---

## 4. Тестирование API

Все запросы идут через **API Gateway** на порту **5000**.  
Замените `SERVER_IP` на IP-адрес вашего сервера.

### 4.1 Регистрация пользователя

```bash
curl -s -X POST http://SERVER_IP:5000/api/account/register \
  -H "Content-Type: application/json" \
  -d '{"userName": "testuser", "password": "Test1234!"}' | jq
```

Ожидаемый ответ:
```json
{
  "id": "...",
  "userName": "testuser",
  "token": "eyJ..."
}
```

### 4.2 Авторизация (получение JWT токена)

```bash
TOKEN=$(curl -s -X POST http://SERVER_IP:5000/api/account/login \
  -H "Content-Type: application/json" \
  -d '{"userName": "testuser", "password": "Test1234!"}' | jq -r '.token')

echo "JWT Token: $TOKEN"
```

### 4.3 Получение профиля пользователя

```bash
curl -s http://SERVER_IP:5000/api/account/user \
  -H "Authorization: Bearer $TOKEN" | jq
```

### 4.4 Список магазинов

```bash
curl -s http://SERVER_IP:5000/api/shops/ \
  -H "Authorization: Bearer $TOKEN" | jq
```

Ожидаемый ответ — 3 магазина:
```json
[
  {"id": 1, "address": "Москва, ул. Первозданного 36", "phoneNumber": "79788994545"},
  {"id": 2, "address": "Орёл, ул. Маршалла 36",        "phoneNumber": "79788992113"},
  {"id": 3, "address": "Москва, ул. Первомайская 36",  "phoneNumber": "79788992553"}
]
```

### 4.5 Товары в магазине

```bash
# Товары магазина id=1
curl -s http://SERVER_IP:5000/api/shops/1 \
  -H "Authorization: Bearer $TOKEN" | jq
```

### 4.6 Поиск товаров по категории

```bash
# Категории: "одежда", "обувь", "еда", "строительные материалы"
curl -s -X POST http://SERVER_IP:5000/api/shops/1/find_by_category \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"category": "одежда"}' | jq
```

### 4.7 Покупка товара

```bash
# Купить товар id=1 (1 штука) в магазине id=1
curl -s -X POST http://SERVER_IP:5000/api/shops/1/order \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '[{"productId": 1, "count": 1}]' | jq
```

Ожидаемый ответ — чек покупки.

### 4.8 История покупок

```bash
# Все покупки текущего пользователя
curl -s http://SERVER_IP:5000/api/purchases/ \
  -H "Authorization: Bearer $TOKEN" | jq
```

### 4.9 Покупка по ID

```bash
# Замените {id} на реальный id из предыдущего запроса
curl -s http://SERVER_IP:5000/api/purchases/{id} \
  -H "Authorization: Bearer $TOKEN" | jq
```

### 4.10 Добавить покупку вручную (без магазина)

```bash
curl -s -X POST http://SERVER_IP:5000/api/purchases/add \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "transactionType": 0,
    "products": [
      {"name": "Тестовый товар", "cost": 99.99, "count": 2, "category": "прочее"}
    ]
  }' | jq
```

---

## 5. Swagger UI (документация API)

Swagger доступен напрямую для каждого сервиса:

| Сервис | URL |
|--------|-----|
| Identity API | `http://SERVER_IP:7001/swagger` |
| Purchases API | `http://SERVER_IP:7002/swagger` |
| Shops API | `http://SERVER_IP:7003/swagger` |

---

## 6. Мониторинг и диагностика

### RabbitMQ Management UI

```
http://SERVER_IP:15672
Логин: guest
Пароль: guest
```

### Логи сервисов

```bash
# Все сервисы
docker compose logs -f

# Конкретный сервис
docker compose logs -f identityapi
docker compose logs -f shopsapi
docker compose logs -f db
```

### Перезапуск одного сервиса

```bash
docker compose restart identityapi
```

### Перезапуск всего стека

```bash
docker compose down
docker compose up -d
```

---

## 7. Обновление кода

```bash
cd microlabb
git pull

cd src
docker compose -f docker-compose.yml up --build -d
```

---

## 8. Остановка и очистка

```bash
# Остановить контейнеры (данные сохраняются)
docker compose -f docker-compose.yml down

# Остановить и удалить данные SQL Server (полный сброс!)
docker compose -f docker-compose.yml down -v
```

---

## 9. Полный smoke-тест (один скрипт)

```bash
#!/bin/bash
set -e
BASE_URL="http://SERVER_IP:5000"

echo "=== Регистрация ==="
curl -sf -X POST "$BASE_URL/api/account/register" \
  -H "Content-Type: application/json" \
  -d '{"userName":"smoketest","password":"Smoke1234!"}' | jq

echo "=== Логин ==="
TOKEN=$(curl -sf -X POST "$BASE_URL/api/account/login" \
  -H "Content-Type: application/json" \
  -d '{"userName":"smoketest","password":"Smoke1234!"}' | jq -r '.token')
echo "Token: $TOKEN"

echo "=== Магазины ==="
curl -sf "$BASE_URL/api/shops/" -H "Authorization: Bearer $TOKEN" | jq

echo "=== Товары магазина 1 ==="
curl -sf "$BASE_URL/api/shops/1" -H "Authorization: Bearer $TOKEN" | jq

echo "=== Покупка ==="
curl -sf -X POST "$BASE_URL/api/shops/1/order" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '[{"productId":1,"count":1}]' | jq

echo "=== История покупок ==="
curl -sf "$BASE_URL/api/purchases/" -H "Authorization: Bearer $TOKEN" | jq

echo "=== Smoke-test PASSED ==="
```

---

## 10. Частые проблемы

### Сервис не запускается — база не готова

```bash
docker compose logs db | tail -20
```

SQL Server инициализируется ~30–60 секунд. Если сервис упал — он перезапустится автоматически (`restart: unless-stopped`). Можно подождать 1–2 минуты и проверить снова.

### Ошибка `Login failed for user 'sa'`

Проверьте, что `SA_PASSWORD` в `.env` совпадает со значением, использованным при первом запуске. Если менялся пароль, нужно полный сброс:

```bash
docker compose down -v
docker compose up -d
```

### RabbitMQ не отвечает

```bash
docker compose logs rabbit | tail -20
docker compose restart rabbit
```

### Ошибка healthcheck: `sqlcmd not found`

Это происходит на SQL Server 2022. Обновите путь в `docker-compose.yml`:

```yaml
# Замените строку healthcheck test в сервисе db:
test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"$$SA_PASSWORD\" -Q 'SELECT 1' -b -o /dev/null"]
```
