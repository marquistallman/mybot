#!/bin/bash

echo "=========================================="
echo "   INICIANDO JARVIS - CONFIGURACIÓN"
echo "=========================================="

# 1. Solicitar credenciales interactivamente
read -r -p "Token del Bot: " TOKEN
read -r -p "Ruta de la plantilla (.docx): " PLANTILLA

# 2. Validar entrada
if [ -z "$TOKEN" ] || [ -z "$PLANTILLA" ]; then
    echo "Error: El token y la ruta son obligatorios."
    exit 1
fi

# 3. Crear archivo de configuración temporal
printf "BOT_TOKEN=%s\n" "$TOKEN" > bot.config
printf "TEMPLATE_PATH=%s\n" "$PLANTILLA" >> bot.config

# 4. Ejecutar el bot (El código C# borrará el config al salir)
echo "Iniciando aplicación..."
dotnet run