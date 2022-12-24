using UnityEditor;
using UnityEngine;


namespace Editor {
	public sealed class AssetInfo {
		public AssetInfo (string guid) => Guid = guid;

		public string  Guid       {get;}
		public string  Path       => AssetDatabase.GUIDToAssetPath(Guid);
		public string  Name       => System.IO.Path.GetFileNameWithoutExtension(Path);
		public Texture Icon       => AssetDatabase.GetCachedIcon(Path);
		public Object  Asset      => AssetDatabase.LoadMainAssetAtPath(Path);
		public int     InstanceId => Asset.GetInstanceID();
	}
}
