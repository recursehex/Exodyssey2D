using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField] private  int inventoryIndex;
	[SerializeField] private  Image ItemImage;
	[SerializeField] private Sprite ItemBackgroundSprite;
	[SerializeField] private Player Player;
	[SerializeField] private Tilemap TilemapGround;
	private bool isDraggable = true;
	private Vector3 OriginalPosition;
	private Transform ParentAfterDrag;
	private Canvas Canvas;
	private RectTransform RectTransform;
	private CanvasGroup CanvasGroup;
	private Button Button;
	void Awake()
	{
		RectTransform = GetComponent<RectTransform>();
		CanvasGroup = GetComponent<CanvasGroup>();
		Canvas = GetComponentInParent<Canvas>();
		Button = GetComponent<Button>();
	}
	public void OnBeginDrag(PointerEventData eventData)
	{
		// If no item is equipped, don't allow dragging
		if (ItemImage.sprite == ItemBackgroundSprite)
		{
			isDraggable = false;
			return;
		}
		isDraggable = true;
		OriginalPosition = RectTransform.localPosition;
		ParentAfterDrag = transform.parent;
		transform.SetParent(transform.root);
		transform.SetAsLastSibling();
		if (CanvasGroup != null)
		{
			CanvasGroup.blocksRaycasts = false;
		}
		if (Button != null)
		{
			// Disable the button interaction during drag
			Button.interactable = false;
		}
	}
	public void OnDrag(PointerEventData eventData)
	{
		if (!isDraggable)
		{
			return;
		}
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			Canvas.transform as RectTransform,
			eventData.position,
			eventData.pressEventCamera,
			out Vector2 localPointerPosition);
		RectTransform.localPosition = localPointerPosition;
	}
	public void OnEndDrag(PointerEventData eventData)
	{
		if (!isDraggable)
		{
			return;
		}
		// Get cell bounds and int position of dragged item
		BoundsInt CellBounds = TilemapGround.cellBounds;
		Vector3Int RectTransformInt = Vector3Int.FloorToInt(RectTransform.localPosition);
		// Drop item if let go within cell bounds
		if (CellBounds.Contains(RectTransformInt))
		{
			// Try to drop the item
			Player.TryDropItem(inventoryIndex);
		}
		// Return the icon to its original position
		RectTransform.localPosition = OriginalPosition;
		transform.SetParent(ParentAfterDrag);
		transform.SetAsLastSibling();
		if (CanvasGroup != null)
		{
			CanvasGroup.blocksRaycasts = true;
		}
		if (Button != null)
		{
			// Delay re-enabling to ensure the Button's OnClick isn't triggered accidentally
			StartCoroutine(EnableButtonAfterFrame());
		}
	}
	private IEnumerator EnableButtonAfterFrame()
	{
		// Wait for the next frame
		yield return null;
		Button.interactable = true;
	}
}