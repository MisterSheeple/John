﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Generator;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Textures;
using OpenTK;
using BlockID = System.UInt16;

namespace ClassicalSharp {
	
	public unsafe partial class ChunkMeshBuilder {
		
		protected DrawInfo[] normalParts, translucentParts;
		protected int arraysCount = 0;
		protected bool fullBright, tinted;
		protected float invVerTileSize;
		protected int tilesPerAtlas1D;
		
		void TerrainAtlasChanged(object sender, EventArgs e) {
			int newArraysCount = TerrainAtlas1D.TexIds.Length;
			if (arraysCount == newArraysCount) return;
			arraysCount = newArraysCount;
			Array.Resize(ref normalParts, arraysCount);
			Array.Resize(ref translucentParts, arraysCount);
			
			for (int i = 0; i < normalParts.Length; i++) {
				if (normalParts[i] != null) continue;
				normalParts[i] = new DrawInfo();
				translucentParts[i] = new DrawInfo();
			}
		}
		
		public void Dispose() {
			game.Events.TerrainAtlasChanged -= TerrainAtlasChanged;
		}
		
		protected class DrawInfo {
			public int[] vIndex = new int[6], vCount = new int[6];
			public int spriteCount, sIndex, sAdvance;
			
			public int VerticesCount() {
				int count = spriteCount;
				for (int i = 0; i < vCount.Length; i++) { count += vCount[i]; }
				return count;
			}
			
			public void CalcOffsets(ref int offset) {
				sIndex   = offset;
				sAdvance = spriteCount / 4;
				
				vIndex[Side.Left]   = spriteCount + offset;
				vIndex[Side.Right]  = vIndex[Side.Left]   + vCount[Side.Left];
				vIndex[Side.Front]  = vIndex[Side.Right]  + vCount[Side.Right];
				vIndex[Side.Back]   = vIndex[Side.Front]  + vCount[Side.Front];
				vIndex[Side.Bottom] = vIndex[Side.Back]   + vCount[Side.Back];
				vIndex[Side.Top]    = vIndex[Side.Bottom] + vCount[Side.Bottom];
				
				offset += VerticesCount();
			}
			
			public void ResetState() {
				spriteCount = 0; sIndex = 0; sAdvance = 0;
				for (int i = 0; i < Side.Sides; i++) {
					vIndex[i] = 0; vCount[i] = 0;
				}
			}
		}

		protected abstract void RenderTile(int index);
		
		protected virtual void PreStretchTiles(int x1, int y1, int z1) {
			invVerTileSize  = TerrainAtlas1D.invTileSize;
			tilesPerAtlas1D = TerrainAtlas1D.TilesPerAtlas;
			arraysCount = TerrainAtlas1D.TexIds.Length;
			
			if (normalParts == null) {
				normalParts = new DrawInfo[arraysCount];
				translucentParts = new DrawInfo[arraysCount];
				for (int i = 0; i < normalParts.Length; i++) {
					normalParts[i] = new DrawInfo();
					translucentParts[i] = new DrawInfo();
				}
			} else {
				for (int i = 0; i < normalParts.Length; i++) {
					normalParts[i].ResetState();
					translucentParts[i].ResetState();
				}
			}
		}
		
		int TotalVerticesCount() {
			int vertsCount = 0;
			for (int i = 0; i < normalParts.Length; i++) {
				vertsCount += normalParts[i].VerticesCount();
				vertsCount += translucentParts[i].VerticesCount();
			}
			return vertsCount;
		}
		
		protected virtual void PostStretchTiles(int x1, int y1, int z1) {
			int vertsCount = TotalVerticesCount();
			if (vertices == null || (vertsCount + 2) > vertices.Length) {
				vertices = new VertexP3fT2fC4b[vertsCount + 2];
				// ensure buffer is up to 64 bits aligned for last element
			}
			
			// organised as: all N_0, all T_0, all N_1, all T1, etc
			vertsCount = 0;
			for (int i = 0; i < normalParts.Length; i++) {
				normalParts[i].CalcOffsets(ref vertsCount);		
				translucentParts[i].CalcOffsets(ref vertsCount);
			}
		}
		
		void AddSpriteVertices(BlockID block) {
			int i = TerrainAtlas1D.Get1DIndex(BlockInfo.GetTextureLoc(block, Side.Left));
			DrawInfo part = normalParts[i];
			part.spriteCount += 4 * 4;
		}
		
		void AddVertices(BlockID block, int face) {
			int i = TerrainAtlas1D.Get1DIndex(BlockInfo.GetTextureLoc(block, face));
			DrawInfo part = BlockInfo.Draw[block] == DrawType.Translucent ? translucentParts[i] : normalParts[i];
			part.vCount[face] += 4;
		}
		
		static JavaRandom spriteRng = new JavaRandom(0);
		protected virtual void DrawSprite(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Right];
			int i = texId / tilesPerAtlas1D;
			float vOrigin = (texId % tilesPerAtlas1D) * invVerTileSize;
			
			float x1 = X + 2.50f/16, y1 = Y,     z1 = Z + 2.50f/16;
			float x2 = X + 13.5f/16, y2 = Y + 1, z2 = Z + 13.5f/16;
			const float u1 = 0, u2 = 15.99f/16f;
			float v1 = vOrigin, v2 = vOrigin + invVerTileSize * 15.99f/16f;
			
			byte offsetType = BlockInfo.SpriteOffset[curBlock];
			if (offsetType >= 6 && offsetType <= 7) {
				spriteRng.SetSeed((X + 1217 * Z) & 0x7fffffff);
				float valX = spriteRng.Next(-3, 3 + 1) / 16.0f;
				float valY = spriteRng.Next(0,  3 + 1) / 16.0f;
				float valZ = spriteRng.Next(-3, 3 + 1) / 16.0f;
				
				const float stretch = 1.7f / 16.0f;
				x1 += valX - stretch; x2 += valX + stretch;
				z1 += valZ - stretch; z2 += valZ + stretch;
				if (offsetType == 7) { y1 -= valY; y2 -= valY; }
			}
			
			DrawInfo part = normalParts[i];
			int col = fullBright ? FastColour.WhitePacked : light.LightCol_Sprite_Fast(X, Y, Z);
			if (tinted) col = TintBlock(curBlock, col);
			VertexP3fT2fC4b v; v.Colour = col;
			
			// Draw Z axis
			int index = part.sIndex;
			v.X = x1; v.Y = y1; v.Z = z1; v.U = u2; v.V = v2; vertices[index + 0] = v;
			v.Y = y2;                     v.V = v1; vertices[index + 1] = v;
			v.X = x2;           v.Z = z2; v.U = u1;           vertices[index + 2] = v;
			v.Y = y1;                     v.V = v2; vertices[index + 3] = v;
			
			// Draw Z axis mirrored
			index += part.sAdvance;
			v.X = x2; v.Y = y1; v.Z = z2; v.U = u2;           vertices[index + 0] = v;
			v.Y = y2;                     v.V = v1; vertices[index + 1] = v;
			v.X = x1;           v.Z = z1; v.U = u1;           vertices[index + 2] = v;
			v.Y = y1;                     v.V = v2; vertices[index + 3] = v;

			// Draw X axis
			index += part.sAdvance;
			v.X = x1; v.Y = y1; v.Z = z2; v.U = u2;           vertices[index + 0] = v;
			v.Y = y2;                     v.V = v1; vertices[index + 1] = v;
			v.X = x2;           v.Z = z1; v.U = u1;           vertices[index + 2] = v;
			v.Y = y1;                     v.V = v2; vertices[index + 3] = v;
			
			// Draw X axis mirrored
			index += part.sAdvance;
			v.X = x2; v.Y = y1; v.Z = z1; v.U = u2;           vertices[index + 0] = v;
			v.Y = y2;                     v.V = v1; vertices[index + 1] = v;
			v.X = x1;           v.Z = z2; v.U = u1;           vertices[index + 2] = v;
			v.Y = y1;                     v.V = v2; vertices[index + 3] = v;
			
			part.sIndex += 4;
		}
		
		protected int TintBlock(BlockID curBlock, int col) {
			FastColour fogCol = BlockInfo.FogColour[curBlock];
			FastColour newCol = FastColour.Unpack(col);
			newCol *= fogCol;
			return newCol.Pack();
		}
	}
}