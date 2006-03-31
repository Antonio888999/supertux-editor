using System;
using System.Collections.Generic;
using OpenGl;
using DataStructures;
using SceneGraph;
using Gtk;
using Gdk;

public class ObjectsEditor : IEditor
{
	private class ControlPoint : IObject, Node
	{
		public enum AttachPoint {
			TOP = 1,
			BOTTOM = 2,
			LEFT = 4,
			RIGHT = 8
		};
		
		private RectangleF area;
		private AttachPoint attachPoint;
		public IObject Object;
		
		private static float DISTANCE = 12;
		private static float SIZE = 16;
		
		public ControlPoint(IObject Object, AttachPoint attachPoint)
		{
			this.Object = Object;
			this.attachPoint = attachPoint;
		}
		
		public void Draw()
		{
			UpdatePosition();
			gl.Color4f(0, 0, 1, 0.7f);
			gl.Disable(gl.TEXTURE_2D);
			
			gl.Begin(gl.QUADS);
			gl.Vertex2f(Area.Left, Area.Top);
			gl.Vertex2f(Area.Right, Area.Top);
			gl.Vertex2f(Area.Right, Area.Bottom);
			gl.Vertex2f(Area.Left, Area.Bottom);
			gl.End();
			
			gl.Enable(gl.TEXTURE_2D);
			gl.Color4f(1, 1, 1, 1);			
		}
		
		public void UpdatePosition()
		{
			Vector pos;
			if((attachPoint & AttachPoint.TOP) != 0) {
				pos.Y = Object.Area.Top - DISTANCE;
			} else if((attachPoint & AttachPoint.BOTTOM) != 0) {
				pos.Y = Object.Area.Bottom + DISTANCE;	
			} else {
				pos.Y = (Object.Area.Top + Object.Area.Bottom) / 2f;
			}
			if((attachPoint & AttachPoint.LEFT) != 0) {
				pos.X = Object.Area.Left - DISTANCE;
			} else if((attachPoint & AttachPoint.RIGHT) != 0) {
				pos.X = Object.Area.Right + DISTANCE;
			} else {
				pos.X = (Object.Area.Left + Object.Area.Right) / 2f;
			}			
			area = new RectangleF(pos.X - SIZE/2f, pos.Y - SIZE/2f, SIZE, SIZE);			
		}
		
		public void ChangeArea(RectangleF Area)
		{
			float adjust = SIZE/2f + DISTANCE;
			
			RectangleF newArea = Object.Area;
			if((attachPoint & AttachPoint.TOP) != 0) {
				newArea.Top = Area.Top + adjust;
			} else if((attachPoint & AttachPoint.BOTTOM) != 0) {
				newArea.Bottom = Area.Bottom - adjust;	
			}
			if((attachPoint & AttachPoint.LEFT) != 0) {
				newArea.Left = Area.Left + adjust;
			} else if((attachPoint & AttachPoint.RIGHT) != 0) {
				newArea.Right = Area.Right - adjust;
			}
			Object.ChangeArea(newArea);
			area = Area;
		}
		
		public bool Resizable {
			get {
				return false;
			}
		}
		
		public RectangleF Area {
			get {
				return area;	
			}
		}
		
		public Node GetSceneGraphNode() {
			return this;
		}
	}
	
	private Sector Sector;
	private IObject ActiveObject;
	private Vector pressPoint;
	private RectangleF originalArea;
	private bool dragging;
	private List<ControlPoint> controlPoints = new List<ControlPoint>();

	public event RedrawEventHandler Redraw;
	
	public ObjectsEditor(Sector Sector)
	{
		this.Sector = Sector;
	}
	
	public void Draw()
	{
		if(ActiveObject != null) {
			IObject obj = ActiveObject;
			if(obj is ControlPoint)
				obj = ((ControlPoint) obj).Object;

			gl.Color4f(1, 0, 0, 0.7f);
			obj.GetSceneGraphNode().Draw();
			gl.Color4f(1, 1, 1, 1);
		}
		foreach(ControlPoint point in controlPoints) {
			point.Draw();
		}
	}

	public void OnMouseButtonPress(Vector pos, int button, ModifierType Modifiers)
	{
		if(button == 1) {
			pressPoint = pos;
			ActiveObject = null;
			foreach(ControlPoint point in controlPoints) {
				if(point.Area.Contains(pos)) {
					ActiveObject = point;
					break;
				}
			}
			
			if(ActiveObject == null) {
				controlPoints.Clear();
				foreach(IObject Object in Sector.GetObjects(typeof(IObject))) {
					if(Object.Area.Contains(pos)) {
						ActiveObject = Object;
						break;
					}
				}
			}
			
			if(ActiveObject != null) {
				originalArea = ActiveObject.Area;
				dragging = true;
				if(ActiveObject.Resizable) {
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.TOP | ControlPoint.AttachPoint.LEFT));
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.TOP));
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.TOP | ControlPoint.AttachPoint.RIGHT));
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.LEFT));
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.RIGHT));
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.BOTTOM | ControlPoint.AttachPoint.LEFT));
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.BOTTOM));
					controlPoints.Add(new ControlPoint(ActiveObject,
                           ControlPoint.AttachPoint.BOTTOM | ControlPoint.AttachPoint.RIGHT));					
				}
			}
			Redraw();
		}
		if(button == 3) {
			PopupMenu(button);		
		}
	}
	
	private void PopupMenu(int button)
	{
		if(! (ActiveObject is IGameObject))
			return;
		
		Menu popupMenu = new Menu();
		MenuItem item = new ImageMenuItem(Stock.Properties, null);
		item.Activated += OnProperties;
		popupMenu.Append(item);
		
		MenuItem deleteItem = new ImageMenuItem(Stock.Delete, null);
		deleteItem.Activated += OnDelete;
		popupMenu.Append(deleteItem);
		
		popupMenu.ShowAll();
		popupMenu.Popup(); 
	}
	
	private void OnProperties(object o, EventArgs args)
	{
		if(ActiveObject == null)
			return;
		new SettingsWindow(ActiveObject.GetType().Name + " Object Properties", ActiveObject);
	}
	
	private void OnDelete(object o, EventArgs args)
	{
		if(ActiveObject == null)
			return;
		Sector.Remove((IGameObject) ActiveObject);
		ActiveObject = null;
	}

	public void OnMouseButtonRelease(Vector pos, int button, ModifierType Modifiers)
	{
		if(dragging) {
			moveObject(pos, (Modifiers & ModifierType.ShiftMask) != 0);
			dragging = false;
		}
	}

	public void OnMouseMotion(Vector pos, ModifierType Modifiers)
	{
		if(dragging) {
			moveObject(pos, (Modifiers & ModifierType.ShiftMask) != 0);
		}
	}
	
	private void moveObject(Vector mousePos, bool snap)
	{
		Vector spos = new Vector(originalArea.Left, originalArea.Top);
		spos += mousePos - pressPoint;
		if(snap) {
			spos = new Vector((float) ((int)spos.X / 32) * 32,
		                      (float) ((int)spos.Y / 32) * 32);	
		}
		
		RectangleF newArea = new RectangleF(spos.X, spos.Y,
		                                       originalArea.Width,
		                                       originalArea.Height);
		ActiveObject.ChangeArea(newArea);
		Redraw();		
	}
}

