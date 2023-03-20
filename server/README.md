# Leap Brush Server

## Run

### Mac, Linux

- `go run cmd/leapbrush-server/*.go`

    - `--help` for usage
    - Add `--verbose` for verbose logging

### Windows PowerShell

- `Start-Process -Wait -NoNewWindow -FilePath "C:\Program Files\Go\bin\go.exe" -ArgumentList (@('run') + @(get-item cmd\leapbrush-server\*.go))`

## Run the test client

- `go run cmd/test-client/main.go --name TestUser1`

## Package for release

- Note: Requires a mac computer in order to create universal mac binaries.
- `./scripts/build.py`

## Regenerate protocol buffer apis.

- Note: also run the associated api update script for the client project.
- `./api/generate_protos.py`
