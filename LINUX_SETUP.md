# Configuración en Linux

Para que el bot funcione correctamente en Linux, especialmente la conversión de Word a PDF sin licencias de pago, es necesario instalar **LibreOffice**.

## 1. Instalar Dependencias

El bot utiliza el comando `soffice` en segundo plano. Ejecuta esto en tu terminal:

```bash
sudo apt-get update
sudo apt-get install libreoffice
```

## 2. Ejecutar el Bot

Usa el script incluido para iniciar el bot de forma segura (sin guardar credenciales en disco):

```bash
chmod +x start_bot.sh
./start_bot.sh
```