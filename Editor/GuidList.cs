using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace sergei_liubich.favourites_window.Editor {
	[Serializable]
	public sealed class GuidList {
		public List <string> guids;

		public GuidList (List <string> guids) {
			this.guids = guids;
		}

		public static List <string> GetGuids () {
			return EditorPrefs.HasKey(Favourites.EditorPrefsKey)
				? JsonUtility.FromJson <GuidList>(EditorPrefs.GetString(Favourites.EditorPrefsKey)).guids
				: new List <string>();
		}
	}
}
