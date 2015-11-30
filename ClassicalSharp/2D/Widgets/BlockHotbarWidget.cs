﻿using System;
using System.Drawing;
using ClassicalSharp.GraphicsAPI;
using OpenTK.Input;

namespace ClassicalSharp {
	
	public sealed class BlockHotbarWidget : Widget {
		
		public BlockHotbarWidget( Game game ) : base( game ) {
			HorizontalAnchor = Anchor.Centre;
			VerticalAnchor = Anchor.BottomOrRight;
			hotbarCount = game.Inventory.Hotbar.Length;
		}
		
		int hotbarCount;
		Texture selTex, backTex;
		float barHeight, selBlockSize, elemSize;
		float barXOffset, borderSize;
		
		public override bool HandlesKeyDown( Key key ) {
			if( key >= Key.Number1 && key <= Key.Number9 ) {
				game.Inventory.HeldBlockIndex = (int)key - (int)Key.Number1;
				return true;
			}
			return false;
		}
		
		public override void Init() {
			float scale = 2 * game.GuiScale;
			selBlockSize = (float)Math.Ceiling( 24 * scale );
			barHeight = (int)(22 * scale);		
			Width = (int)(182 * scale);
			Height = (int)barHeight;
			
			elemSize = 16 * scale;
			barXOffset = 3 * scale;
			borderSize = 4 * scale;
			X = game.Width / 2 - Width / 2;
			Y = game.Height - Height;
			
			MakeBackgroundTexture();
			MakeSelectionTexture();
		}
		
		public override void Render( double delta ) {
			graphicsApi.Texturing = true;
			RenderHotbar();
			graphicsApi.BindTexture( game.TerrainAtlas.TexId );
			
			for( int i = 0; i < hotbarCount; i++ ) {
				byte block = (byte)game.Inventory.Hotbar[i];
				int x = (int)(X + barXOffset + (elemSize + borderSize) * i + elemSize / 2);
				int y = (int)(game.Height - barHeight / 2);
				
				float scale = (elemSize - 6) / 2f;
				IsometricBlockDrawer.Draw( game, block, scale, x, y );
			}
			graphicsApi.Texturing = false;
		}
		
		void RenderHotbar() {
			backTex.Render( graphicsApi );
			int i = game.Inventory.HeldBlockIndex;
			int x = (int)(X + barXOffset + (elemSize + borderSize) * i + elemSize / 2);
			selTex.X1 = (int)(x - selBlockSize / 2);
			selTex.Render( graphicsApi );
		}
		
		public override void Dispose() { }
		
		public override void MoveTo( int newX, int newY ) {
			int diffX = newX - X, diffY = newY - Y;
			X = newX; Y = newY;
			Dispose();
			Init();
		}
		
		void MakeBackgroundTexture() {
			TextureRec rec = new TextureRec( 0, 0, 182/256f, 22/256f );
			backTex = new Texture( game.GuiTexId, X, Y, Width, Height, rec );
		}
		
		void MakeSelectionTexture() {
			int size = (int)selBlockSize;
			int y = game.Height - size;
			TextureRec rec = new TextureRec( 0, 22/256f, 24/256f, 24/256f );
			selTex = new Texture( game.GuiTexId, 0, y,  size, size, rec );
		}
	}
}