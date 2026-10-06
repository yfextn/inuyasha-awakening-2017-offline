using XEngine;

namespace InuyashaLocalServer;

internal delegate void Handler(Conn c, Packet pkt, object req);
