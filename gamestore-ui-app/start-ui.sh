#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")"
node ./serve-ui.js


cd "C:\Users\baris\OneDrive\Belgeler\github\epam\gameProject\game-store-v2-with-branches"

dotnet run `
  --project Gamestore\Gamestore.API\Gamestore.API.csproj `
  --launch-profile https

  cd "C:\Users\baris\OneDrive\Belgeler\github\epam\gameProject\game-store-v2-with-branches\microservice\publish"
.\WebApi.exe