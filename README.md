# Opengram

[![API Layer](https://img.shields.io/badge/Уровень_API-211-blueviolet)](https://corefork.telegram.org/methods)
[![MTProto](https://img.shields.io/badge/Протокол_MTProto-2.0-green)](https://corefork.telegram.org/mtproto/)
[![Канал Opengram](https://img.shields.io/badge/Opengram-%D0%9F%D0%BE%D0%B4%D0%BF%D0%B8%D1%81%D0%B0%D1%82%D1%8C%D1%81%D1%8F-0088cc)](https://t.me/opengrame)

**Opengram** — это реализация backend сервера Telegram, на C#. Основано на Free версии **[MyTelegram](https://github.com/loyldg/mytelegram)**

**Opengram** is telegram server side api implementation written in c#, support private deployment. Based on Free version **[MyTelegram](https://github.com/loyldg/mytelegram)**

## 📋 Фичи

- API Layer: **`211`**
- [MTProto transports](https://corefork.telegram.org/mtproto/mtproto-transports): **`Abridged`**,**`Intermediate`**
- 💬 Приватные чаты
- 👥 Групповые чаты
- 🔊 Супергруппы
- 📢 Каналы
- 🔐 Сквозное шифрование
- 📞 Голосовые/видеозвонки
- 🤖 Боты
- 🔒 2FA
- 🎨 Стикеры
- ❤️ Реакции
- 📝 Топики
- 🎭 Темы оформления/Обои/Автоудаление сообщений/Отложенные сообщения/Списки чатов/Telegram для бизнеса/Истории/Вход по email/Отправка email/Личные сообщения/Push (Firebase)

## 🚀 Запуск

### 🐳 Opengram Docker

1. **docker-compose:**
   ```bash
   wget https://raw.githubusercontent.com/zavolo/opengram/main/docker/compose/docker-compose.yml
   wget https://raw.githubusercontent.com/zavolo/opengram/main/docker/compose/.env
   ```

2. **Смените IP:**
   Замените `192.168.1.100` на IP вашего сервера в `.env`

3. **Выполните** в директории с файлом docker-compose.yml:
   ```bash
   mkdir -p ./data/mytelegram
   chown -R a+w ./data/mytelegram
   docker compose up
   ```

4. **Код верификации:** `22222`

## ⚙️ Конфигурация

В файле `.env` можно настроить следующие параметры:

### 🏷️ Брендинг
```env
# Название мессенджера
App__Brand=Opengram

# Приветственное сообщение для новых пользователей (если включено)
App__WelcomeMsg=Welcome to use Opengram!
```

### 🔒 Защищенные юзернеймы
Список юзернеймов, которые нельзя зарегистрировать обычным пользователям (поменять в случае если зарегистрировано):
```env
App__ProtectedUsernames__0=admin
App__ProtectedUsernames__1=botfather
# Добавляйте дополнительные защищенные имена по индексам, тоже самое проделайте с docker-compose.yml
```

**Что это дает:**
- **Brand** - заменяет название "Opengram" на ваше
- **WelcomeMsg** - настраивает приветственное сообщение для новых пользователей
- **ProtectedUsernames** - блокирует регистрацию системных и важных имен пользователей

## 🔧 Docker Opengram

### Linux/amd64
```bash
build-all-amd64.sh
```

### Linux/arm64
```bash
build-all-arm64.sh
```

## 📱 Клиенты

- **[TDesktop Opengram](https://github.com/loyldg/mytelegram-tdesktop)**
- **[Android Opengram](https://github.com/loyldg/mytelegram-android)**
- **[iOS Opengram](https://github.com/loyldg/mytelegram-iOS)**
- **[WebK Opengram](https://github.com/loyldg/mytelegram-webk)**
- **[WebA Opengram](https://github.com/loyldg/mytelegram-weba)**
- Заменить **`192.168.1.100`** во всех файлах, на ваш IP и поменяйте **[RSA PUBLIC KEY](https://github.com/loyldg/mytelegram-android/commit/fe5ecd463d69e64717612b8e81c2263e585ccac6)**

## ❤️ Поддержать Opengram

Поставьте звёздочку репозиторию ⭐ или поддержите материально [https://opengra.me/donate](https://opengra.me/donate)

## 📞 Обратная связь

**Канал:** [https://t.me/opengrame](https://t.me/opengrame)