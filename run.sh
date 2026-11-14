#!/bin/sh
set -eu

project_dir=$(CDPATH= cd -P "$(dirname "$0")" && pwd)
cd "$project_dir"

dotnet_command=dotnet
sdk_major=0

if command -v dotnet >/dev/null 2>&1; then
    sdk_major=$(dotnet --version | cut -d. -f1)
fi

if [ "$sdk_major" -lt 10 ]; then
    dotnet_command=/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet

    if [ ! -x "$dotnet_command" ]; then
        printf '%s\n' '.NET 10 SDK is required. Install it or use the SDK supplied with Rider.' >&2
        exit 1
    fi
fi

sh native/build.sh
"$dotnet_command" build DeliveryPipeline.sln -c Release --nologo
"$dotnet_command" test DeliveryPipeline.sln -c Release --no-build --nologo
"$dotnet_command" run --project DeliveryPipeline/DeliveryPipeline.csproj -c Release --no-build -- "$@"
