using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public int inventoryIndex;
	private bool isDraggable = true;
	private Vector3 OriginalPosition;
	private Transform ParentAfterDrag;
	private Canvas Canvas;
	private RectTransform RectTransform;
	private CanvasGroup CanvasGroup;
	private Button Button;
	public Image ItemImage;
	public Sprite ItemBackgroundSprite;
	public Player Player;
	
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
		// Try to drop the item
		Player.TryDropItem(inventoryIndex);
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
	private System.Collections.IEnumerator EnableButtonAfterFrame()
	{
		// Wait for the next frame
		yield return null;
		Button.interactable = true;
	}
}