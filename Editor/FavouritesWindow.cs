using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MouseButton = UnityEngine.UIElements.MouseButton;
using Object = UnityEngine.Object;


namespace SergeiLiubich.FavouritesWindow.Editor {
	public sealed class FavouritesWindow : EditorWindow, IHasCustomMenu {
		private const string EditorPrefsKey = "Favourites_fa5a5c9c";

		private static Texture _windowIcon;
		private static Texture _folderIcon;

		private static Favourites _favourites;
		private        ListView   listView;


		private static void LoadIcons () {
			_windowIcon ??= EditorGUIUtility.Load("d_FolderFavorite Icon") as Texture;
			_folderIcon ??= EditorGUIUtility.Load("d_Folder Icon") as Texture;
		}

		[MenuItem("Window/Favourites")]
		public static void OpenWindow () {
			GetWindow <FavouritesWindow>().Show();
		}

		[MenuItem("Assets/Add to Favourites", priority = 9999)]
		private static void AddToFavourites (MenuCommand command) {
			foreach (Object selectedObject in Selection.objects)
				_favourites.Add(new[] { selectedObject });
		}

		private void OnEnable () {
			LoadIcons();

			titleContent = new GUIContent("Favourites") { image = _windowIcon };

			Favourites.Load(out _favourites);
			_favourites.Changed += OnFavouritesChanged;

			EditorApplication.projectChanged -= OnProjectChanged;
			EditorApplication.projectChanged += OnProjectChanged;

			CreteListView();
		}

		private void OnDisable () {
			EditorApplication.projectChanged -= OnProjectChanged;
			_favourites.Changed              -= OnFavouritesChanged;
		}

		private void CreteListView () {
			listView = new ListView(_favourites.Entries, (int)EditorGUIUtility.singleLineHeight, MakeListItem, BindListItem) {
				style = {
					flexGrow     = 1f,
					marginBottom = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
				},
				selectionType = SelectionType.None,
				showBorder    = true
			};

			listView.onSelectionChange += OnListSelectionChanged;
			listView.RegisterCallback <KeyDownEvent>(OnDeleteKeyPressed);

			rootVisualElement.Add(listView);
		}

		private VisualElement MakeListItem () {
			FavouritesListItem listItem = new FavouritesListItem();
			listItem.OnSelected += OnListItemSelected;

			return listItem;
		}

		private static void BindListItem (VisualElement element, int index) {
			if (index >= _favourites.Count)
				return;

			AssetInfo assetInfo = _favourites[index];

			FavouritesListItem listItem = (FavouritesListItem)element;

			listItem.SetData(assetInfo.Name, assetInfo.Icon, index);
		}

		private static void OnListSelectionChanged (IEnumerable <object> selection) {
			SelectAsset((AssetInfo)selection.First());
		}

		private void OnListItemSelected (int itemIndex) {
			listView.SetSelectionWithoutNotify(new[] { itemIndex });
			SelectAsset(_favourites[itemIndex]);
		}

		private void OnFavouritesChanged () {
			listView.Refresh();
		}

		private void OnDeleteKeyPressed (KeyDownEvent @event) {
			if (@event.keyCode != KeyCode.Delete || listView.selectedIndex < 0 || focusedWindow != this)
				return;

			_favourites.RemoveAt(listView.selectedIndex);
		
			@event.StopImmediatePropagation();
			Event.current.Use();
		}

		private void OnProjectChanged () => listView.Refresh();

		private void OnGUI () {
			if (listView.selectedIndex < 0)
				return;

			GUILayout.FlexibleSpace();

			EditorGUIUtility.SetIconSize(new Vector2(16, 16));

			AssetInfo assetInfo = _favourites[listView.selectedIndex];
			string    assetPath = assetInfo.Path;

			GUILayout.Label(new GUIContent(
					assetPath,
					assetInfo.Icon,
					assetPath),
				new GUIStyle(EditorStyles.label) { fixedHeight = EditorGUIUtility.singleLineHeight });
		}

		public void AddItemsToMenu (GenericMenu menu) {
			menu.AddItem(
				EditorGUIUtility.TrTempContent("Clear Favourites"),
				false,
				ClearFavourites
			);
		}

		private static void SelectAsset (AssetInfo assetInfo) {
			Object asset = assetInfo.Object;
			Selection.activeObject = asset;

			// FIXME NullReferenceException
			// EditorGUIUtility.PingObject(asset.GetInstanceID());
		}

		private static void ClearFavourites () {
			const string dialogTitle   = "Clear All Favourites?";
			const string dialogMessage = "Are you sure you want to clear all Favourites?\nYou cannot undo this action.";

			if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "No") == false)
				return;

			_favourites.Clear();
		}

		private sealed class FavouritesAssetPostprocessor : AssetPostprocessor {
			private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
				if (deletedAssets.Length > 0)
					_favourites?.OnAssetsDeleted(deletedAssets);
			}
		}

		private sealed class FavouritesListItem : VisualElement {
			private readonly Label nameLabel;
			private readonly Image iconImage;
			private          int   index;

			private bool isDoubleClicked;

			public event Action <int> OnSelected;


			public FavouritesListItem () {
				style.flexDirection = FlexDirection.Row;

				nameLabel = new Label { pickingMode = PickingMode.Ignore };

				iconImage = new Image {
					style = {
						width      = 16,
						height     = 16,
						flexShrink = 0
					},
					pickingMode = PickingMode.Ignore
				};

				Add(iconImage);
				Add(nameLabel);

				RegisterCallback <MouseDownEvent>(OnMouseDown);
				RegisterCallback <MouseUpEvent>(OnMouseUp);
				RegisterCallback <MouseMoveEvent>(OnMouseMove);
			}

			public void SetData (string itemName, Texture icon, int index) {
				iconImage.image = icon;
				nameLabel.text  = itemName;
				this.index      = index;
			}

			private void OnMouseDown (MouseDownEvent @event) {
				switch ((MouseButton)@event.button) {
					case MouseButton.LeftMouse:
						if (@event.clickCount > 1) {
							isDoubleClicked = true;
							break;
						}

						KillEvent(@event);
						break;
					case MouseButton.RightMouse:
					case MouseButton.MiddleMouse:
					default:
						break;
				}
			}

			private void OnMouseUp (MouseUpEvent @event) {
				switch ((MouseButton)@event.button) {
					case MouseButton.LeftMouse:
						if (isDoubleClicked) {
							OnDoubleClicked();
							break;
						}

						OnSelected?.Invoke(index);

						KillEvent(@event);
						break;
					case MouseButton.RightMouse:
						ShowPopupMenu();

						KillEvent(@event);
						break;
					case MouseButton.MiddleMouse:
					default:
						break;
				}
			}

			private void OnMouseMove (MouseMoveEvent @event) {
				if (@event.button != (int)MouseButton.LeftMouse)
					return;

				if (Event.current.type != EventType.MouseDrag)
					return;

				DragAndDrop.PrepareStartDrag();
				DragAndDrop.objectReferences = new[] { _favourites[index].Object };
				DragAndDrop.StartDrag(nameLabel.text);
			}

			private void OnDoubleClicked () {
				AssetInfo assetInfo = _favourites[index];

				if (AssetDatabase.IsValidFolder(assetInfo.Path)) {
					EditorApplication.ExecuteMenuItem("Assets/Show in Explorer");
				} else {
					AssetDatabase.OpenAsset(assetInfo.InstanceId);
				}

				isDoubleClicked = false;
			}

			private void ShowPopupMenu () {
				GenericMenu menu = new GenericMenu();

				menu.AddItem(new GUIContent("Properties..."),    false, ShowProperties);
				menu.AddItem(new GUIContent("Show in Explorer"), false, ShowInExplorer);
				menu.AddSeparator(string.Empty);
				menu.AddItem(new GUIContent("Remove"), false, Remove);

				menu.ShowAsContext();

				Event.current.Use();
			}

			private void Remove () {
				AssetInfo assetInfo = _favourites[index];

				const string dialogTitle   = "Remove from Favourites?";
				string       dialogMessage = $"{assetInfo.Path}\nYou cannot undo this action.";

				if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "No") == false)
					return;

				_favourites.RemoveAt(index);
			}

			private void ShowProperties () {
				Object[] currentSelection = Selection.objects;

				AssetInfo assetInfo = _favourites[index];
				Selection.objects = new[] { assetInfo.Object };
				EditorApplication.ExecuteMenuItem("Assets/Properties...");

				Selection.objects = currentSelection;
			}

			private void ShowInExplorer () {
				Object[] currentSelection = Selection.objects;

				AssetInfo assetInfo = _favourites[index];
				Selection.objects = new[] { assetInfo.Object };
				EditorApplication.ExecuteMenuItem("Assets/Show in Explorer");

				Selection.objects = currentSelection;
			}

			private static void KillEvent (EventBase @event) {
				@event.StopImmediatePropagation();
				Event.current.Use();
			}
		}

		private sealed class Favourites {
			public event Action Changed;

			public readonly List <AssetInfo> Entries;

			public int Count => Entries.Count;

			public AssetInfo this [int index] => Entries[index];

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

			public void RemoveAt (int index) {
				RemoveInternal(new[] { Entries[index] }, true, false);
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

		private sealed class AssetInfo {
			public AssetInfo (string guid) => Guid = guid;

			public string  Guid       {get;}
			public string  Path       => AssetDatabase.GUIDToAssetPath(Guid);
			public string  Name       => System.IO.Path.GetFileNameWithoutExtension(Path);
			public Texture Icon       => AssetDatabase.GetCachedIcon(Path);
			public Object  Object     => AssetDatabase.LoadMainAssetAtPath(Path);
			public int     InstanceId => Object.GetInstanceID();
		}

		[Serializable]
		private sealed class GuidList {
			public List <string> guids;

			public GuidList (List <string> guids) {
				this.guids = guids;
			}

			public static List <string> GetGuids () {
				return EditorPrefs.HasKey(EditorPrefsKey)
					? JsonUtility.FromJson <GuidList>(EditorPrefs.GetString(EditorPrefsKey)).guids
					: new List <string>();
			}
		}
	}
}
