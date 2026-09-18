using UnityEngine;

namespace SupersonicWisdomSDK
{
    using Newtonsoft.Json;

    internal class SwVisualElementPayload
    {
        #region --- Properties ---
        
        [JsonProperty(nameof(Name))] public string Name { get; set; }

        [JsonProperty(nameof(Font))] public string Font { get; set; }

        [JsonProperty(nameof(Text))] public string Text { get; set; }

        [JsonProperty(nameof(FontSize))] public string FontSize { get; set; }

        [JsonProperty(nameof(FontColor))] public string FontColor { get; set; }

        [JsonProperty(nameof(Margin))] public string Margin { get; set; }

        [JsonProperty(nameof(Padding))] public string Padding { get; set; }

        [JsonProperty(nameof(BackgroundColor))] public string BackgroundColor { get; set; }

        [JsonProperty(nameof(BackgroundImage))] public Sprite BackgroundImage { get; set; }
        
        #endregion
        
        
        #region --- Construction ---

        public SwVisualElementPayload() { }

        public SwVisualElementPayload(string jsonPayload)
        {
            FromJson(jsonPayload);
        }
        
        #endregion
        
        
        #region --- Public Methods ---

        internal void FromJson(string jsonPayload)
        {
            try
            {
                SwVisualElementPayload fromJson = SwUtils.JsonHandler.DeserializeObject<SwVisualElementPayload>(jsonPayload);

                Name = fromJson.Name;
                Font = fromJson.Font;
                Text = fromJson.Text;
                FontSize = fromJson.FontSize;
                FontColor = fromJson.FontColor;
                Margin = fromJson.Margin;
                Padding = fromJson.Padding;
                BackgroundColor = fromJson.BackgroundColor;
                BackgroundImage = fromJson.BackgroundImage;
            }
            catch (JsonSerializationException ex)
            {
                SwInfra.Logger.LogException(ex, EWisdomLogType.UiToolkit, $"Provided JSON payload could not be deserialized to {nameof(SwVisualElementPayload)}. Please check the JSON.");
            }
        }

        internal string ToJson()
        {
            return SwUtils.JsonHandler.SerializeObject(this);
        }
        
        #endregion
    }
}