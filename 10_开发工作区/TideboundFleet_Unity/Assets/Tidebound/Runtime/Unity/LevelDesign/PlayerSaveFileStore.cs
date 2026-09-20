using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Save;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Atomic full-profile snapshot. A failed pre-commit write leaves the previous profile authoritative.</summary>
    public sealed class PlayerSaveFileStore : IPlayerSaveStore
    {
        private readonly string path;
        private readonly Action beforeReplace;
        public PlayerSaveFileStore(string path,Action beforeReplace=null) {this.path=path ?? throw new ArgumentNullException(nameof(path));this.beforeReplace=beforeReplace;}
        private sealed class Envelope {public int EnvelopeVersion=1;public string Payload,Digest;}
        private static string Digest(string value) {using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));}
        public PlayerSaveData Load()
        {
            if(!File.Exists(path)){if(File.Exists(path+".bak"))throw new InvalidDataException("Main save is missing; preserve backup for recovery.");return null;}
            var envelope=JsonConvert.DeserializeObject<Envelope>(File.ReadAllText(path));
            if(envelope==null || envelope.EnvelopeVersion!=1 || envelope.Payload==null || Digest(envelope.Payload)!=envelope.Digest)throw new InvalidDataException("Invalid player save envelope.");
            JObject json;
            // Preserve receipt timestamp strings exactly; JObject defaults may coerce them to Date tokens.
            using(var reader=new JsonTextReader(new StringReader(envelope.Payload)){DateParseHandling=DateParseHandling.None})json=JObject.Load(reader);
            if(json["Version"]?.Type!=JTokenType.Integer)throw new InvalidDataException("Missing save version.");
            if((int)json["Version"]==PlayerSaveData.CurrentVersion)
            {
                var collection=json["Collection"] as JObject;
                var required=new[]{"Version","CatalogVersion","RulesVersion","ProfileSeed","OwnedIds","Equipment","Tickets","TotalDraws","GoldDry","RedDry","DuplicateDry","FirstBlueClaimed","Receipts","EquipmentReceipts"};
                if(collection==null || required.Any(k=>collection[k]==null || collection[k].Type==JTokenType.Null))throw new InvalidDataException("Incomplete collection profile.");
            }
            var data=json.ToObject<PlayerSaveData>();if(data==null)throw new InvalidDataException("Missing player save.");data.Validate();return data;
        }
        public void Save(PlayerSaveData data)
        {
            data.Validate();var payload=JsonConvert.SerializeObject(data);var bytes=Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Envelope{Payload=payload,Digest=Digest(payload)}));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));var temp=path+".tmp";
            try
            {
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                beforeReplace?.Invoke();
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }
            finally {if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
