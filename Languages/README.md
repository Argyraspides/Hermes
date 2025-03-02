The format of the incoming JSON from the MAVLinkInterface.py script is as follows:

```json
{
    "msgid" : 33,
    "sysid" : 1,
    "compid" : 1,
    "sequence" : 224,
    "payload" : {
        "mavpackettype" : "GLOBAL_POSITION_INT",
        "time_boot_ms" : 22299760,
        "lat" : 473979704,
        "lon" : 85461630,
        "alt" : -573,
        "relative_alt" : 319,
        "vx" : -4,
        "vy" : 0,
        "vz" : 25,
        "hdg" : 8282
    }
}
```

The message will always contain:

- Message ID (``msgid``)
- System ID (``sysid``)
- Component ID (``compid``)
- Sequence Number (``sequence``)
- Payload (``payload``)
    - The payload will contain a special field ``mavpackettype`` indicating the name of the MAVLink message

The custom generator scripts assume this format.

### TODO(Argyraspides, 02/03/2025): The format should have a single source of truth somewhere eventually. Right now this format is hardcoded into the MAVLinkInterface.py script.
