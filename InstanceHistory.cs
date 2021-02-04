using MelonLoader;
using System.Collections;
using System.Reflection;
using System.IO;
using System;
using System.Linq;
using VRC.SDKBase;
using UIExpansionKit.API;

[assembly: MelonInfo(typeof(KiraiMod.InstanceHistory), "InstanceHistory", "1", "Kirai Chan#8315", "github.com/xKiraiChan/InstanceHistory")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace KiraiMod
{
    public class InstanceHistory : MelonMod
    {
        #region Load KiraiLib
        static InstanceHistory()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
#if DEBUG || LOCAL
                "KiraiMod.Lib.KiraiLib.dll"
#else
                "KiraiMod.Lib.KiraiLibLoader.dll"
#endif
                );

            MemoryStream mem = new MemoryStream((int)stream.Length);
            stream.CopyTo(mem);

            Assembly.Load(mem.ToArray());

            new Action(() =>
#if DEBUG || LOCAL
                KiraiLib.NoOp()
#else
                KiraiLibLoader.Load()
#endif
            )();
        }
        #endregion

        private object CancellationToken;
        private System.Collections.Generic.List<string> history = new System.Collections.Generic.List<string>();
        private object labels;
        private int page;

        const string path = "UserData/kInstanceHistory.txt";

        public override void OnApplicationStart()
        {
            labels = new System.Collections.Generic.List<KiraiLib.UI.Label>();
            
            if (File.Exists(path))
                history = File.ReadAllText(path).Split('\n').ToList();

            KiraiLib.Callbacks.OnUIUnload += () => (labels as System.Collections.Generic.List<KiraiLib.UI.Label>).Clear();
            KiraiLib.Callbacks.OnUIReload += () => Draw();
        }


        public override void OnSceneWasLoaded(int level, string sceneName)
        {
            if (level == -1)
            {
                if (CancellationToken != null)
                    MelonCoroutines.Stop(CancellationToken);
                CancellationToken = MelonCoroutines.Start(WaitForRoomManager());
            }
        }

        public override void VRChat_OnUiManagerInit()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Instance History", () => { KiraiLib.UI.selected = page; });

            KiraiLib.UI.Initialize();

            Draw();
        }

        private IEnumerator WaitForRoomManager()
        {
            while (RoomManager.field_Internal_Static_ApiWorld_0 is null) yield return null;
            CancellationToken = null;

            string room = $"{RoomManager.field_Internal_Static_ApiWorld_0.id}:{RoomManager.field_Internal_Static_ApiWorld_0.currentInstanceIdWithTags}";
            string name = RoomManager.field_Internal_Static_ApiWorld_0.name.Trim();

            if (name.Length > 35)
                name = room.Substring(0, 32).Trim() + "...";

            AddToHistory($"{name}#{room}");
        }

        public void AddToHistory(string wrld)
        {
            history.Insert(0, wrld);
            history = history.Distinct().ToList();

            while (history.Count > 12)
                history.RemoveAt(11);

            string serialized = "";
            for (int i = 0; i < history.Count(); i++)
            {
                serialized += history[i] + "\n";
            }
            serialized = serialized.Substring(0, serialized.Length - 1);

            File.WriteAllText(path, serialized);

            Redraw();
        }

        public void Draw()
        {
            page = KiraiLib.UI.CreatePage("kInstanceHistory");

            Redraw();
        }

        public void Redraw()
        {
            foreach (KiraiLib.UI.Label label in labels as System.Collections.Generic.List<KiraiLib.UI.Label>)
                label.Destroy();
            (labels as System.Collections.Generic.List<KiraiLib.UI.Label>).Clear();

            for (int i = 0; i < history.Count; i++)
            {
                string[] tmp = history[i].Split('#');

                if (tmp.Length != 2)
                {
                    history.Remove(history[i]);
                    continue;
                }

                string name = tmp[0];
                string id = tmp[1];
                
                string instance = id.Split(':')[1]; // unsafe but this wont be a problem unless someone calls this method themself

                if (instance.Length > 5)
                    instance = instance.Substring(0, 5);

                // note: this is a special character
                (labels as System.Collections.Generic.List<KiraiLib.UI.Label>).Add(
                KiraiLib.UI.Label.Create($"kih/{i + 1}", $"{i + 1} ({instance.PadRight(5, ' ')}) {name}", 400, 800 - i * 100, KiraiLib.UI.pages[page].transform, () =>
                {
                    JoinWorldById(id);
                }, false));
            }
        }

        public static bool JoinWorldById(string id)
        {
            if (!Networking.GoToRoom(id))
            {
                string[] split = id.Split(':');

                if (split.Length != 2) return false;

                new PortalInternal().Method_Private_Void_String_String_PDM_0(split[0], split[1]);
            }

            return true;
        }
    }
}
