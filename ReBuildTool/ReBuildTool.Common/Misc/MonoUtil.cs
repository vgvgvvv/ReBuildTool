namespace UnityCompiler.Internal;

public class MonoUtil
{
	public static bool IsDotNetAssembly(string filePath)
	{
		using (var stream = File.OpenRead(filePath))
		using (var reader = new BinaryReader(stream))
		{
			if (stream.Length < 128)
			{
				return false;
			}
			// read DOSHeader
			if (reader.ReadUInt16() != 0x5a4d)
			{
				return false;
			}
			
			stream.Seek (58, SeekOrigin.Current);
			stream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);
		
			if (reader.ReadUInt32() != 0x00004550)
			{
				return false;
			}

			// PEFileHeader
			// Machine				2
			reader.ReadUInt16();
			// NumberOfSections		2
			reader.ReadUInt16 ();
			// TimeDateStamp		4
			reader.ReadUInt32 ();
			// PointerToSymbolTable	4
			// NumberOfSymbols		4
			// OptionalHeaderSize	2
			stream.Seek (10, SeekOrigin.Current);
			// Characteristics		2
			reader.ReadUInt16 ();
			
			// - PEOptionalHeader
			//   - StandardFieldsHeader

			bool pe64 = reader.ReadUInt16() == 0x20b;

			// pe32 || pe64
			reader.ReadUInt16 ();
			
			// CodeSize				4
			// InitializedDataSize	4
			// UninitializedDataSize4
			// EntryPointRVA		4
			// BaseOfCode			4
			// BaseOfData			4 || 0

			//   - NTSpecificFieldsHeader

			// ImageBase			4 || 8
			// SectionAlignment		4
			// FileAlignement		4
			// OSMajor				2
			// OSMinor				2
			// UserMajor			2
			// UserMinor			2
			// SubSysMajor			2
			// SubSysMinor			2
			stream.Seek (44, SeekOrigin.Current);
			
			reader.ReadUInt16 ();
			reader.ReadUInt16 ();
			
			// Reserved				4
			// ImageSize			4
			// HeaderSize			4
			// FileChecksum			4
			stream.Seek (16, SeekOrigin.Current);

			// SubSystem			2
			reader.ReadUInt16();
			
			// DLLFlags				2
			reader.ReadUInt16 ();
			// StackReserveSize		4 || 8
			// StackCommitSize		4 || 8
			// HeapReserveSize		4 || 8
			// HeapCommitSize		4 || 8
			// LoaderFlags			4
			// NumberOfDataDir		4

			//   - DataDirectoriesHeader

			// ExportTable			8
			// ImportTable			8
			
			stream.Seek (pe64 ? 56 : 40, SeekOrigin.Current);
			
			// ResourceTable		8

			
			reader.ReadUInt32();
			reader.ReadUInt32();
			
			// ExceptionTable		8
			// CertificateTable		8
			// BaseRelocationTable	8
			
			stream.Seek (24, SeekOrigin.Current);
			
			// Debug				8
			reader.ReadUInt32();
			reader.ReadUInt32();
			
			// Copyright			8
			// GlobalPtr			8
			// TLSTable				8
			// LoadConfigTable		8
			// BoundImport			8
			// IAT					8
			// DelayImportDescriptor8
			stream.Seek (56, SeekOrigin.Current);
			
			// CLIHeader			8
			var rva = reader.ReadUInt32();
			var size = reader.ReadUInt32();

			if (rva == 0 && size == 0)
			{
				return false;
			}
		}

		return true;
	}
}