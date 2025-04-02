using Photon.Client;

namespace ReadyM.Relay.Client.Serialization;

// TODO: Reference our StreamBuffer
public delegate object DeserializeStreamMethod(StreamBuffer inStream, short length);