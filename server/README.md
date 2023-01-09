# Leap Brush Server

## Run

- `go run cmd/leapbrush-server/*.go`

    - `--help` for usage
    - Add `--verbose` for verbose logging

## Run the test client

- `go run cmd/test-client/main.go --name TestUser1`

## Package for release

- Note: Requires a mac computer in order to create universal mac binaries.
- `./scripts/build.py`

## Regenerate protocol buffer apis.

- Note: also run the associated api update script for the client project.
- `./api/generate_protos.py`
