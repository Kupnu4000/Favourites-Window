using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace sergei_liubich.favourites_window.Editor {
	public sealed class FavouritesListItem : VisualElement {
		private Label nameLabel;
		private Image iconImage;

		private bool isDoubleClicked;

		public event Action <AssetInfo>          RemovalRequested;
		public event Action <FavouritesListItem> Selected;

		public FavouritesListItem () {
			this.AddManipulator(new ContextualMenuManipulator(PopulateContextMenu));

			style.flexDirection = FlexDirection.Row;

			AddIcon();
			AddLabel();

			RegisterCallback <MouseDownEvent>(OnMouseDown);
			RegisterCallback <MouseUpEvent>(OnMouseUp);
			RegisterCallback <MouseMoveEvent>(OnMouseMove);
		}

		public AssetInfo AssetInfo {get; private set;}

		private void AddIcon () {
			iconImage = new Image {
				pickingMode = PickingMode.Ignore,
				style = {
					width      = 16,
					height     = 16,
					flexShrink = 0
				}
			};

			Add(iconImage);
		}

		private void AddLabel () {
			nameLabel = new Label { pickingMode = PickingMode.Ignore };
			Add(nameLabel);
		}

		public void SetAssetInfo (AssetInfo assetInfo) {
			if (assetInfo == null)
				return;

			AssetInfo = assetInfo;

			iconImage.image = assetInfo.Icon;
			nameLabel.text  = assetInfo.Name;
		}

		private void PopulateContextMenu (ContextualMenuPopulateEvent @event) {
			DropdownMenu dropdownMenu = @event.menu;

			dropdownMenu.AppendAction("Properties...",    ShowProperties);
			dropdownMenu.AppendAction("Show in Explorer", ShowInExplorer);
			dropdownMenu.AppendSeparator();
			dropdownMenu.AppendAction("Remove", Remove);
		}

		private void OnMouseDown (MouseDownEvent @event) {
			if (@event.button != (int)MouseButton.LeftMouse)
				return;

			if (@event.clickCount > 1)
				isDoubleClicked = true;
		}

		private void OnMouseUp (MouseUpEvent @event) {
			if (@event.clickCount > 1 || @event.button != (int)MouseButton.LeftMouse)
				return;

			if (isDoubleClicked) {
				Choose();
				return;
			}

			Select();
		}

		private void OnMouseMove (MouseMoveEvent @event) {
			if (@event.button != (int)MouseButton.LeftMouse || Event.current.type != EventType.MouseDrag)
				return;

			DragAndDrop.PrepareStartDrag();
			DragAndDrop.objectReferences = new[] { AssetInfo.Asset };
			DragAndDrop.StartDrag(nameLabel.text);
		}

		private void Select () {
			PingAssetInProjectBrowsers();
			Selection.activeObject = AssetInfo.Asset;
			Selected?.Invoke(this);
		}

		private void PingAssetInProjectBrowsers () {
			Type       projectBrowserType    = typeof(EditorGUIUtility).Assembly.GetType("UnityEditor.ProjectBrowser");
			MethodInfo getAllProjectBrowsers = projectBrowserType.GetMethod("GetAllProjectBrowsers", BindingFlags.Public | BindingFlags.Static);

			if (getAllProjectBrowsers == null)
				return;

			foreach (object projectBrowser in (IEnumerable)getAllProjectBrowsers.Invoke(null, null)) {
				MethodInfo frameObject = projectBrowser.GetType().GetMethod("FrameObject", BindingFlags.Public);

				if (frameObject == null)
					continue;

				frameObject.Invoke(projectBrowser, new object[] { AssetInfo.InstanceId, true });
			}
		}

		private void Choose () {
			Select();

			if (AssetDatabase.IsValidFolder(AssetInfo.Path)) {
				EditorApplication.ExecuteMenuItem("Assets/Show in Explorer");
			} else {
				AssetDatabase.OpenAsset(AssetInfo.InstanceId);
			}

			isDoubleClicked = false;
		}

		private void ShowProperties (DropdownMenuAction _) {
			Object[] currentSelection = Selection.objects;

			Selection.objects = new[] { AssetInfo.Asset };
			EditorApplication.ExecuteMenuItem("Assets/Properties...");

			Selection.objects = currentSelection;
		}

		private void ShowInExplorer (DropdownMenuAction _) {
			Object[] currentSelection = Selection.objects;

			Selection.objects = new[] { AssetInfo.Asset };
			EditorApplication.ExecuteMenuItem("Assets/Show in Explorer");

			Selection.objects = currentSelection;
		}

		private void Remove (DropdownMenuAction _) {
			const string dialogTitle   = "Remove from Favourites?";
			string       dialogMessage = $"{AssetInfo.Path}\nYou cannot undo this action.";

			if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "No") == false)
				return;

			RemovalRequested?.Invoke(AssetInfo);
		}
	}
}
