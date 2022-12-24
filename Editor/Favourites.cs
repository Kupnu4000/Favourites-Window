using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace sergei_liubich.favourites_window.Editor {
	internal sealed class Favourites {
		public const string EditorPrefsKey = "Favourites_fa5a5c9c";

		public event Action Changed;

		public readonly List <AssetInfo> Entries;

		public AssetInfo this [int index] => Entries[index];

		public int Count => Entries.Count;


		private Favourites (IEnumerable <string> guids) {
			Entries = guids.Select(guid => new AssetInfo(guid))
			               .Where(entry => AssetDatabase.GetMainAssetTypeAtPath(entry.Path) != null)
			               .ToList();

			Sort();
		}

		public void Add (IEnumerable <Object> objects) {
			AddInternal(objects, true, false);
		}

		private void AddInternal (IEnumerable <Object> objects, bool save, bool silent) {
			List <string> guids = GuidList.GetGuids();

			bool isChanged = false;

			foreach (Object obj in objects) {
				string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));

				if (guids.Contains(guid))
					continue;

				Entries.Add(new AssetInfo(guid));
				guids.Add(guid);

				isChanged = true;
			}

			if (isChanged == false)
				return;

			Sort();

			if (save)
				Save(guids);

			if (silent == false)
				Changed?.Invoke();
		}

		public void Remove (AssetInfo assetInfo) {
			RemoveInternal(new[] { assetInfo }, true, false);
		}

		private void RemoveInternal (IEnumerable <AssetInfo> assetInfos, bool save, bool silent) {
			List <string> guids = GuidList.GetGuids();

			foreach (AssetInfo assetInfo in assetInfos.ToArray()) {
				Entries.Remove(assetInfo);
				guids.Remove(assetInfo.Guid);
			}

			if (save)
				Save(guids);

			if (silent == false)
				Changed?.Invoke();
		}

		private void Sort () {
			Entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

			Entries.Sort((a, b) => string.Compare(Path.GetExtension(a.Path), Path.GetExtension(b.Path), StringComparison.OrdinalIgnoreCase));
		}

		public void Clear () {
			RemoveInternal(Entries, true, true);
			Entries.Clear();

			Changed?.Invoke();
		}

		private static void Save (List <string> guids) {
			string json = JsonUtility.ToJson(new GuidList(guids), true);
			EditorPrefs.SetString(EditorPrefsKey, json);
		}

		public static void Load (out Favourites favourites) {
			favourites = new Favourites(GuidList.GetGuids());
			favourites.Changed?.Invoke();
		}

		public void OnAssetsDeleted (string[] deletedAssets) {
			IEnumerable <AssetInfo> deletedAssetInfos = Entries.Where(entry => deletedAssets.Contains(entry.Path));

			RemoveInternal(deletedAssetInfos, true, false);
		}
	}
}
