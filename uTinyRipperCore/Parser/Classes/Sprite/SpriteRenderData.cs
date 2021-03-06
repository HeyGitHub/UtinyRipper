﻿using System.Collections.Generic;
using System.IO;
using uTinyRipper.Classes.Meshes;
using uTinyRipper.Classes.Sprites.Utils;
using uTinyRipper.SerializedFiles;

namespace uTinyRipper.Classes.Sprites
{
	public struct SpriteRenderData : IAssetReadable, IDependent
	{
		/// <summary>
		/// 5.2.0 and greater
		/// </summary>
		public static bool IsReadAlphaTexture(Version version)
		{
			return version.IsGreaterEqual(5, 2);
		}
		/// <summary>
		/// Less than 5.6.0
		/// </summary>
		public static bool IsReadVertices(Version version)
		{
			return version.IsLess(5, 6);
		}
		/// <summary>
		/// 2018.1 and greater
		/// </summary>
		public static bool IsReadBindpose(Version version)
		{
			return version.IsGreaterEqual(2018);
		}
		/// <summary>
		/// 2018.1
		/// </summary>
		public static bool IsReadSourceSkin(Version version)
		{
			return version.IsEqual(2018, 1);
		}
		/// <summary>
		/// 5.4.5p1 to 5.5.0 exclusive or 5.5.0p3 or 5.5.3 and greater
		/// </summary>
		public bool IsReadAtlasRectOffset(Version version)
		{
			return (version.IsGreaterEqual(5, 4, 5, VersionType.Patch, 1) && version.IsLess(5, 5)) ||
				version.IsEqual(5, 5, 0, VersionType.Patch, 3) || version.IsGreaterEqual(5, 5, 2, VersionType.Patch);
		}
		/// <summary>
		/// 4.5.0 and greater
		/// </summary>
		public static bool IsReadUVTransform(Version version)
		{
			return version.IsGreaterEqual(4, 5);
		}
		/// <summary>
		/// 2017.1 and greater
		/// </summary>
		public static bool IsReadDownscaleMultiplier(Version version)
		{
			return version.IsGreaterEqual(2017);
		}
		
		public Vector2f[][] GenerateOutline(Version version)
		{
			if(IsReadVertices(version))
			{
				Vector2f[][] outline = new Vector2f[1][];
				outline[0] = new Vector2f[Vertices.Count];
				for (int i = 0; i < Vertices.Count; i++)
				{
					outline[0][i] = Vertices[i].Position.ToVector2();
				}
				return outline;
			}
			else
			{
				List<Vector2f[]> outlines = new List<Vector2f[]>();
				foreach(SubMesh submesh in SubMeshes)
				{
					Vector3f[] vertices = VertexData.GenerateVertices(version, submesh);
					VerticesToOutline(outlines, vertices, submesh);
				}
				return outlines.ToArray();
			}
		}

		public void Read(AssetReader reader)
		{
			Texture.Read(reader);
			if (IsReadAlphaTexture(reader.Version))
			{
				AlphaTexture.Read(reader);
			}

			if (IsReadVertices(reader.Version))
			{
				m_vertices = reader.ReadArray<SpriteVertex>();
				m_indices = reader.ReadUInt16Array();
				reader.AlignStream(AlignType.Align4);
			}
			else
			{
				m_subMeshes = reader.ReadArray<SubMesh>();
				m_indexBuffer = reader.ReadByteArray();
				reader.AlignStream(AlignType.Align4);

				VertexData.Read(reader);
			}
			if (IsReadBindpose(reader.Version))
			{
				m_bindpose = reader.ReadArray<Matrix4x4f>();
			}
			if(IsReadSourceSkin(reader.Version))
			{
				m_sourceSkin = reader.ReadArray<BoneWeights4>();
			}

			TextureRect.Read(reader);
			TextureRectOffset.Read(reader);
			if(IsReadAtlasRectOffset(reader.Version))
			{
				AtlasRectOffset.Read(reader);
			}
			SettingsRaw = reader.ReadUInt32();
			if(IsReadUVTransform(reader.Version))
			{
				UVTransform.Read(reader);
			}
			if(IsReadDownscaleMultiplier(reader.Version))
			{
				DownscaleMultiplier = reader.ReadSingle();
			}
		}

		public IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)
		{
			yield return Texture.FetchDependency(file, isLog, () => nameof(SpriteRenderData), "Texture");
			yield return AlphaTexture.FetchDependency(file, isLog, () => nameof(SpriteRenderData), "AlphaTexture");
		}

		private void VerticesToOutline(List<Vector2f[]> outlines, Vector3f[] vertices, SubMesh submesh)
		{
			int triangleCount = submesh.IndexCount / 3;
			List<Vector3i> triangles = new List<Vector3i>(triangleCount);
			using (MemoryStream memStream = new MemoryStream(m_indexBuffer))
			{
				using (BinaryReader reader = new BinaryReader(memStream))
				{
					memStream.Position = submesh.FirstByte;
					for (int i = 0; i < triangleCount; i++)
					{
						int x = reader.ReadInt16();
						int y = reader.ReadInt16();
						int z = reader.ReadInt16();
						triangles.Add(new Vector3i(x, y, z));
					}
				}
			}

			MeshOutlineGenerator outlineGenerator = new MeshOutlineGenerator(vertices, triangles);
			List<Vector2f[]> meshOutlines = outlineGenerator.GenerateOutlines();
			outlines.AddRange(meshOutlines);
		}

		public IReadOnlyList<SpriteVertex> Vertices => m_vertices;
		public IReadOnlyList<ushort> Indices => m_indices;
		public IReadOnlyList<SubMesh> SubMeshes => m_subMeshes;
		public IReadOnlyList<byte> IndexBuffer => m_indexBuffer;
		public IReadOnlyList<Matrix4x4f> Bindpose => m_bindpose;
		public IReadOnlyList<BoneWeights4> SourceSkin => m_sourceSkin;
		public uint SettingsRaw { get; private set; }
		public float DownscaleMultiplier { get; private set; }

		public PPtr<Texture2D> Texture;
		public PPtr<Texture2D> AlphaTexture;
		public VertexData VertexData;
		public Rectf TextureRect;
		public Vector2f TextureRectOffset;
		public Vector2f AtlasRectOffset;
		public Vector4f UVTransform;

		private SpriteVertex[] m_vertices;
		private ushort[] m_indices;
		private SubMesh[] m_subMeshes;
		private byte[] m_indexBuffer;
		private Matrix4x4f[] m_bindpose;
		private BoneWeights4[] m_sourceSkin;
	}
}
