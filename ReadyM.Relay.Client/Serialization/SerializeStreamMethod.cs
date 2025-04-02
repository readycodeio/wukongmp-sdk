using Photon.Client;

namespace ReadyM.Relay.Client.Serialization;

// TODO: Reference our StreamBuffer
public delegate short SerializeStreamMethod(StreamBuffer outStream, object customObject);