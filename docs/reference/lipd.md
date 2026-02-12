# lipd

`lipd` is the daemon process for lip, implementing a JSON-RPC 2.0 server over standard I/O (stdin/stdout). It allows programmatic interaction with lip's core functionality.

## Usage

The daemon is typically invoked as a subprocess by the CLI or other tools.

```bash
lipd
```

When started, it listens for JSON-RPC requests on standard input and writes responses to standard output.

## JSON-RPC API

For detailed parameter and return type information, refer to the [OpenRPC schema](https://github.com/futrime/lip/blob/main/schemas/lipd.v3.openrpc.json).
