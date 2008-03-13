//  $Id$
using DataStructures;
using OpenGl;
using System;
using Gdk;
using Undo;
using Drawing;

/// <summary>
/// Smoothes Tilemaps by changing tiles to one of several stored valid patterns.
/// Left-click and drag to apply brush.
/// Right-click and drag to select an area with patterns to learn.
/// </summary>
public sealed class BrushEditor : TileEditorBase, IEditor {
	private new Selection selection = new Selection();
	private Brush brush;

	/// <summary>
	/// Contains position where last preview was generated.
	/// </summary>
	private FieldPos LastPreviewPos;
	/// <summary>
	/// A cache of the preview of changing the current "active" area
	/// </summary>
	private TileBlock LastPreview;
	/// <summary>
	/// Stores if the last preview would be a change or not.
	/// </summary>
	private bool LastPreviewIsChange;

	public event RedrawEventHandler Redraw;

	internal TileBlock.StateData tilemapBackup; // saved OnMouseButtonPress

	internal sealed class TilemapModifyCommand : Command {
		internal Tilemap changedTilemap;
		internal TileBlock.StateData oldState;
		internal TileBlock.StateData newState;

		public override void Do() {
			changedTilemap.Tiles.RestoreState(newState);
		}

		public override void Undo() {
			changedTilemap.Tiles.RestoreState(oldState);
		}

		public TilemapModifyCommand(string title, Tilemap changedTilemap, TileBlock.StateData oldState, TileBlock.StateData newState) : base(title) {
			this.changedTilemap = changedTilemap;
			this.oldState = oldState;
			this.newState = newState;
		}
	}

	public BrushEditor(IEditorApplication application, Tilemap Tilemap, Tileset Tileset, string brushFile)
		: base(application, Tilemap, Tileset) {
		selection = new Selection();
		selection.Changed += OnSelectionChanged;
		brush = Brush.loadFromFile(brushFile, Tileset);
	}

	/// <summary>
	/// Brush currently in use
	/// </summary>
	public Brush Brush {
		get {
			return brush;
		}
	}

	/// <summary>
	/// Updates the LastPreview if the current mouse position has changed.
	/// </summary>
	private void UpdatePreview() {
		if (LastPreviewPos != MouseTilePos) {
			LastPreviewIsChange = brush.FindBestPattern(MouseTilePos, Tilemap, ref LastPreview);
			LastPreviewPos = MouseTilePos;
		}
	}

	public new void Draw(DrawingContext context)
	{
		/* TODO
		// When not selecting, draw white rectangle over affected tiles
		if(!selecting) {

			// Calculate rectangle to color
			float px = (MouseTilePos.X - (int)(brush.Width / 2)) * 32f;
			float py = (MouseTilePos.Y - (int)(brush.Height / 2)) * 32f;
			float w = brush.Width * 32f;
			float h = brush.Height * 32f;

			// Draw rectangle
			gl.Color4f(1, 1, 1, 0.25f);
			gl.Disable(gl.TEXTURE_2D);
			gl.Begin(gl.QUADS);
			gl.Vertex2f(px, py);
			gl.Vertex2f(px+w, py);
			gl.Vertex2f(px+w, py+h);
			gl.Vertex2f(px, py+h);
			gl.End();
			gl.Enable(gl.TEXTURE_2D);

			// Draw a preview if we can.
			UpdatePreview();
			if ((LastPreview != null) && (px > 0) && (py > 0)) {
				gl.Color4f(1, 1, 1, 0.7f);
				Vector pos = new Vector(px, py);
				LastPreview.Draw(pos, Tileset);
			}

			// Draw a red rectangle around if the preview is a change
			if (LastPreviewIsChange) {
				gl.Color4f(1, 0, 0, 1);
				gl.Disable(gl.TEXTURE_2D);
				gl.PolygonMode(gl.FRONT_AND_BACK, gl.LINE);

				gl.Begin(gl.QUADS);
				gl.Vertex2f(px, py);
				gl.Vertex2f(px+w, py);
				gl.Vertex2f(px+w, py+h);
				gl.Vertex2f(px, py+h);
				gl.End();

				gl.PolygonMode(gl.FRONT_AND_BACK, gl.FILL);
				gl.Enable(gl.TEXTURE_2D);

			}
			gl.Color4f(1, 1, 1, 1);
		}

		// When selecting, draw blue rectangle over selected area
		if(selecting) {

			// Calculate rectangle to color
			float left = SelectionP1.X * 32f;
			float top = SelectionP1.Y * 32f;
			float right = SelectionP2.X * 32f + 32f;
			float bottom = SelectionP2.Y * 32f + 32f;

			// Draw rectangle
			gl.Color4f(0, 0, 1, 0.7f);
			gl.Disable(gl.TEXTURE_2D);
			gl.Begin(gl.QUADS);
			gl.Vertex2f(left, top);
			gl.Vertex2f(right, top);
			gl.Vertex2f(right, bottom);
			gl.Vertex2f(left, bottom);
			gl.End();
			gl.Enable(gl.TEXTURE_2D);
			gl.Color4f(1, 1, 1, 1);

		}
		*/
	}

	public void OnMouseButtonPress(Vector mousePos, int button, ModifierType Modifiers)
	{
		UpdateMouseTilePos(mousePos);

		// left mouse button means apply brush
		if(button == 1) {

			// save backup of Tilemap
			tilemapBackup = Tilemap.Tiles.SaveState();

			brush.ApplyToTilemap(MouseTilePos, Tilemap);
			LastDrawPos = MouseTilePos;
			drawing = true;
			Redraw();
		}

		// right mouse button means select area to learn
		if(button == 3) {
			if(MouseTilePos.X < 0 || MouseTilePos.Y < 0
				|| MouseTilePos.X >= Tilemap.Width
				|| MouseTilePos.Y >= Tilemap.Height)
				return;

			SelectStartPos = MouseTilePos;
			selecting = true;
			UpdateSelection();
			Redraw();
		}
	}

	public void OnMouseButtonRelease(Vector mousePos, int button, ModifierType Modifiers)
	{
		UpdateMouseTilePos(mousePos);

		// left mouse button means apply brush
		if(button == 1) {
			drawing = false;

			// use backup of Tilemap to create undo command
			TilemapModifyCommand command = new TilemapModifyCommand("Tile Brush on Tilemap \""+Tilemap.Name+"\"", Tilemap, tilemapBackup, Tilemap.Tiles.SaveState());
			UndoManager.AddCommand(command);

		}

		// right mouse button means select area to learn
		if(button == 3) {
			UpdateSelection();

			uint NewWidth = (uint) (SelectionP2.X - SelectionP1.X) + 1;
			uint NewHeight = (uint) (SelectionP2.Y - SelectionP1.Y) + 1;
			selection.Resize(NewWidth, NewHeight, 0);
			for(uint y = 0; y < NewHeight; y++) {
				for(uint x = 0; x < NewWidth; ++x) {
					selection[x, y]
						= Tilemap.Tiles[(uint) SelectionP1.X + x,
						                (uint) SelectionP1.Y + y];
				}
			}
			brush.LearnPatterns(selection);

			selection.FireChangedEvent();
			selecting = false;
		}

		Redraw();
	}

	public void OnMouseMotion(Vector mousePos, ModifierType Modifiers)
	{
		if (UpdateMouseTilePos(mousePos)) {
			if (drawing) {
				if (LastDrawPos != MouseTilePos) {
					LastDrawPos = MouseTilePos;
					brush.ApplyToTilemap(MouseTilePos, Tilemap);
				}
			}
			if (selecting) {
				UpdateSelection();
			}
			Redraw();
		}
	}

	private void OnSelectionChanged() {
		Redraw();
	}
}