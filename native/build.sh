#!/bin/sh
set -eu

native_dir=$(CDPATH= cd -P "$(dirname "$0")" && pwd)

if [ "$(uname -s)" != "Darwin" ] || [ "$(uname -m)" != "arm64" ]; then
    printf '%s\n' 'The native build supports macOS on Apple Silicon (arm64).' >&2
    exit 1
fi

clang -std=c11 -Wall -Wextra -Werror -O2 -dynamiclib -arch arm64 \
    "$native_dir/deliverymath.c" -o "$native_dir/libdeliverymath.dylib"

printf 'Built %s\n' "$native_dir/libdeliverymath.dylib"
