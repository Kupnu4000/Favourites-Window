using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace Editor {
	internal sealed class FavouritesWindow : EditorWindow, IHasCustomMenu {
		private Favourites favourites;

		private static Texture _windowIcon;
		private static Texture _folderIcon;

		private ListView listView;

		private static event Action <Object[]> AddToFavouritesInitiated;
		private static event Action <string[]> AssetsDeleted;

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
			Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
			AddToFavouritesInitiated?.Invoke(selection);
		}

		private void OnEnable () {
			LoadIcons();

			titleContent = new GUIContent("Favourites") { image = _windowIcon };

			Favourites.Load(out favourites);

			EditorApplication.projectChanged -= OnProjectChanged;
			EditorApplication.projectChanged += OnProjectChanged;

			AddToFavouritesInitiated += favourites.Add;
			AssetsDeleted            += favourites.OnAssetsDeleted;
			favourites.Changed       += OnFavouritesChanged;

			CreteListView();
		}

		private void OnDisable () {
			EditorApplication.projectChanged -= OnProjectChanged;

			AddToFavouritesInitiated -= favourites.Add;
			AssetsDeleted            -= favourites.OnAssetsDeleted;
			favourites.Changed       -= OnFavouritesChanged;
		}

		private void CreteListView () {
			listView = new ListView(
				favourites.Entries,
				(int)EditorGUIUtility.singleLineHeight,
				CreateListItem,
				BindListItem
			);

			listView.selectionType  = SelectionType.None;
			listView.style.flexGrow = 1f;
			// listView.style.paddingBottom = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			// listView.style.marginBottom = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			listView.showBorder  = true;
			listView.reorderable = false;

			rootVisualElement.Add(listView);
		}

		private FavouritesListItem CreateListItem () {
			FavouritesListItem listItem = new FavouritesListItem();

			listItem.RemovalRequested += OnItemRemovalRequested;
			listItem.Selected         += OnItemSelected;

			return listItem;
		}

		private void BindListItem (VisualElement element, int index) {
			FavouritesListItem listItem = (FavouritesListItem)element;
			listItem.SetAssetInfo(favourites[index]);
		}

		private void OnItemSelected (FavouritesListItem item) {
			listView.ClearSelection();
			int index = listView.itemsSource.IndexOf(item.AssetInfo);
			listView.SetSelection(index);
		}

		private void OnGUI () {
			if (listView.selectedIndex < 0)
				return;

			DrawPrefabPath();
		}

		private void DrawPrefabPath () {
			GUILayout.FlexibleSpace();

			EditorGUIUtility.SetIconSize(new Vector2(16, 16));

			AssetInfo assetInfo = favourites[listView.selectedIndex];
			string    assetPath = assetInfo.Path;

			GUILayout.Label(new GUIContent(
					assetPath,
					assetInfo.Icon,
					assetPath),
				new GUIStyle(EditorStyles.label) { fixedHeight = EditorGUIUtility.singleLineHeight });
		}

		private void OnItemRemovalRequested (AssetInfo assetInfo) {
			favourites.Remove(assetInfo);
		}

		public void AddItemsToMenu (GenericMenu menu) {
			menu.AddItem(
				EditorGUIUtility.TrTempContent("Clear Favourites"),
				false,
				ClearFavourites
			);
		}

		private void OnFavouritesChanged () {
			listView.Refresh();
		}

		private void OnProjectChanged () {
			listView.Refresh();
		}

		private void ClearFavourites () {
			const string dialogTitle   = "Clear All Favourites?";
			const string dialogMessage = "Are you sure you want to clear all Favourites?\nYou cannot undo this action.";

			if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "No") == false)
				return;

			favourites.Clear();
		}


		internal sealed class FavouritesAssetPostprocessor : AssetPostprocessor {
			private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
				if (deletedAssets.Length > 0)
					AssetsDeleted?.Invoke(deletedAssets);
			}
		}
	}
}
