using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Tidebound.Tools;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Local tool stock only. Atomic replacement; unreadable data is retained and fails closed.</summary>
    public sealed class ToolInventoryFileStore : IToolInventoryStore
    {
        private readonly string path;
        public ToolInventoryFileStore(string path) { this.path=path ?? throw new ArgumentNullException(nameof(path)); }
        private sealed class Envelope { public string Payload; public string Digest; }
        private static string Digest(string text)
        { using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text))); }
        public ToolInventoryData Load()
        {
            if(!File.Exists(path))return null;
            var envelope=JsonConvert.DeserializeObject<Envelope>(File.ReadAllText(path));
            if(envelope==null || envelope.Payload==null || Digest(envelope.Payload)!=envelope.Digest)throw new InvalidDataException("Tool inventory checksum mismatch.");
            var data=JsonConvert.DeserializeObject<ToolInventoryData>(envelope.Payload);
            if(data==null)throw new InvalidDataException("Tool inventory missing.");data.Validate();return data;
        }
        public void Save(ToolInventoryData data)
        {
            data.Validate();var payload=JsonConvert.SerializeObject(data);
            var bytes=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Envelope {Payload=payload,Digest=Digest(payload)}));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var temp=path+".tmp";
            try
            {
                using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
                { file.Write(bytes,0,bytes.Length);file.Flush(true); }
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }
            finally { if(File.Exists(temp))File.Delete(temp); }
        }
    }
}
