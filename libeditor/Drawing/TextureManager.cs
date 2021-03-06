//  SuperTux Editor
//  Copyright (C) 2006 Matthias Braun <matze@braunis.de>
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Resources;
using Gtk;

namespace Drawing
{

	public static class TextureManager
	{
		private static Dictionary<string, ImageTexture> ImageTextures
			= new Dictionary<string, ImageTexture>();

		private static ImageTexture notFoundImageTexture = null;
		private static ImageTexture NotFoundImageTexture {
			get {
				if (notFoundImageTexture != null) {
					return notFoundImageTexture;
				} else {
					notFoundImageTexture = new ImageTexture(ResourceManager.Instance.Get("images/engine/missing.png"));
					return notFoundImageTexture;
				}
			}
		}

		public static ImageTexture Get(string Resourcepath)
		{
			ImageTexture Result;
			if(ImageTextures.ContainsKey(Resourcepath)) {
				Result = ImageTextures[Resourcepath];
			} else {
				Result = CreateImageTexture(Resourcepath);
				ImageTextures.Add(Resourcepath, Result);
			}

			return Result;
		}

		private static ImageTexture CreateImageTexture(string Resourcepath)
		{
			try {
				return new ImageTexture(ResourceManager.Instance.Get(Resourcepath));
			} catch {
				Console.WriteLine($"error: failed to load: {Resourcepath}");
				return NotFoundImageTexture;
			}
		}
	}

}

/* EOF */
