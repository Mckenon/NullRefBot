using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NullRefBot
{
	public class ConfigJson
	{
		[JsonProperty("token")]
		public string Token { get; private set; }

		[JsonProperty("prefix")]
		public string CommandPrefix { get; private set; }

		[JsonProperty("database_ip")]
		public string DatabaseIP { get; private set; }

		[JsonProperty("database_port")]
		public int DatabasePort { get; private set; }

		[JsonProperty("debug_mode")]
		public bool DebugMode { get; private set; }

		[JsonProperty("debug_channels")]
		public ulong[] DebugChannels { get; private set; }

		[JsonProperty("encryption_key")]
		public string EncryptionKey { get; private set; }
	}
}
