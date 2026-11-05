// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace zClock
{
    public sealed class PrompterSettings
    {
        public bool Enabled = true;
        public bool AllowLan = true;
        public bool AutoWindow = true;
        public bool SharePreviews = false;
        public bool AutoRequestFirewall = true;
        public string FirewallRequestFor = "";
        public int Port = 18765;
        public string AccessKey = Guid.NewGuid().ToString("N");
        public float FontSize = 32;
        public bool LightTheme;
        public Rectangle WindowBounds;
        internal static string FilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "zClock", "prompter.xml"); }
        }
        public static PrompterSettings Load()
        {
            try
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var settings = (PrompterSettings)new XmlSerializer(typeof(PrompterSettings)).Deserialize(stream);
                    if (settings.Port < 1024 || settings.Port > 65535) settings.Port = 18765;
                    Guid key;
                    if (!Guid.TryParseExact(settings.AccessKey, "N", out key)) settings.AccessKey = Guid.NewGuid().ToString("N");
                    if (settings.FontSize < 18 || settings.FontSize > 80 || float.IsNaN(settings.FontSize)) settings.FontSize = 32;
                    return settings;
                }
            }
            catch (IOException) { return new PrompterSettings(); }
            catch (InvalidOperationException) { return new PrompterSettings(); }
            catch (UnauthorizedAccessException) { return new PrompterSettings(); }
        }
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temporary = FilePath + ".tmp";
            using (var stream = File.Create(temporary)) new XmlSerializer(typeof(PrompterSettings)).Serialize(stream, this);
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
            else File.Move(temporary, FilePath);
        }
    }
}
