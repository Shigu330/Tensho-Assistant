#!/bin/bash
# Shared by the agent's scripts. TENSHO_DOTNET picks the SDK; otherwise the agent's local one, else the one on the PATH.
if [ -n "$TENSHO_DOTNET" ]; then DOTNET="$TENSHO_DOTNET"
elif [ -x "$HOME/.codex/dotnet-10.0.403/dotnet.exe" ]; then DOTNET="$HOME/.codex/dotnet-10.0.403/dotnet.exe"
else DOTNET="dotnet"; fi
AGENT_TEMP="${TEMP:-/tmp}/tensho-agent"; mkdir -p "$AGENT_TEMP"
