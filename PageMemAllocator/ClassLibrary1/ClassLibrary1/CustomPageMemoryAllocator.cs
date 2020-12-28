using System;
using System.Collections.Generic;
using System.Text;

namespace CustomPageMemoryAllocator
{
    public class MyPageMemoryAllocator
    {

        private const int DEFAULT_SIZE = PHeader.PAGE_TOTAL_SIZE * 10;

        private int _size;
        private byte[] _buffer;


        public MyPageMemoryAllocator()
        {
            _size = DEFAULT_SIZE;
            _buffer = new byte[_size];
            FillPages();
        }

        public MyPageMemoryAllocator(int size)
        {
            this._size = AlignSize(size);
            _buffer = new byte[this._size];
            FillPages();
        }

        private bool CheckIndex(int index)
        {
            return index >= 0 && index < _size - PHeader.PAGE_HEADER_SIZE - BHeader.BLOCK_HEADER_SIZE;
        }

        private int GetPageHeaderIndex(int index)
        {
            int numberOfPage = index / PHeader.PAGE_TOTAL_SIZE;
            return numberOfPage * PHeader.PAGE_TOTAL_SIZE;
        }

        private PHeader GetPageHeader(int index)
        {
            int pageHeaderIndex = GetPageHeaderIndex(index);
            int pageHeaderByteArrayLength = PHeader.PAGE_HEADER_SIZE;
            byte[] pageHeaderByteArray = new byte[pageHeaderByteArrayLength];
            Array.Copy(_buffer, pageHeaderIndex, pageHeaderByteArray, 0, pageHeaderByteArrayLength);

            return new PHeader(pageHeaderByteArray);
        }

        private int GetPageFreeBlockIndex(int index)
        {
            PHeader pageHeader = GetPageHeader(index);
            TypeOfPage pageType = pageHeader.pageType;

            int blockHeaderIndex = index + PHeader.PAGE_HEADER_SIZE;
            while (blockHeaderIndex < index + PHeader.PAGE_TOTAL_SIZE)
            {
                BHeader blockHeader = GetBlockHeader(blockHeaderIndex);
                if (blockHeader.IsFree)
                {
                    return blockHeaderIndex;
                }
                blockHeaderIndex += BHeader.BLOCK_HEADER_SIZE + pageType.Size;
            }
            return -1;
        }
        private BHeader GetBlockHeader(int index)
        {
            int blockHeaderByteArrayLength = BHeader.BLOCK_HEADER_SIZE;
            byte[] blockHeaderByteArray = new byte[blockHeaderByteArrayLength];
            Array.Copy(_buffer, index, blockHeaderByteArray, 0, blockHeaderByteArrayLength);
            return new BHeader(blockHeaderByteArray);
        }

        private int AlignSize(int size)
        {
            return size % PHeader.PAGE_TOTAL_SIZE == 0
                ? size
                : PHeader.PAGE_TOTAL_SIZE * (size / PHeader.PAGE_TOTAL_SIZE + 1);
        }

        private void CreatePageHeader(int index, PHeader pageHeader)
        {
            byte[] pageHeaderByteArray = pageHeader.ToByteArray();
            Array.Copy(pageHeaderByteArray, 0, _buffer, index, pageHeaderByteArray.Length);
        }

        private void CreateBlockHeader(int index, BHeader blockHeader)
        {
            byte[] blockHeaderByteArray = blockHeader.ToByteArray();
            Array.Copy(blockHeaderByteArray, 0, _buffer, index, blockHeaderByteArray.Length);
        }

        private void FillPages()
        {
            PHeader pageHeader = new PHeader();
            byte[] pageHeaderByteArray = pageHeader.ToByteArray();
            int index = 0;
            while (index < _buffer.Length)
            {
                Array.Copy(pageHeaderByteArray, 0, _buffer, index, pageHeaderByteArray.Length);
                index += PHeader.PAGE_TOTAL_SIZE;
            }
        }

        private void FillPage(int index, PHeader pageHeader)
        {
            TypeOfPage pageType = pageHeader.pageType;
            CreatePageHeader(index, pageHeader);
            BHeader blockHeader = new BHeader();
            int blockHeaderIndex = index + PHeader.PAGE_HEADER_SIZE;
            while (blockHeaderIndex < index + PHeader.PAGE_TOTAL_SIZE)
            {
                CreateBlockHeader(blockHeaderIndex, blockHeader);
                blockHeaderIndex += BHeader.BLOCK_HEADER_SIZE + pageType.Size;
            }
        }



        public int MemAllocate(int size)
        {
            TypeOfPage pageType = PHeader.GetTypeBySize(size);


            int pageIndex = 0;


            int index = -1;
            while (index == -1 && pageIndex < _size - PHeader.PAGE_HEADER_SIZE)
            {
                PHeader pageHeader = GetPageHeader(pageIndex);
                TypeOfPage currentPageType = pageHeader.pageType;


                if (currentPageType == pageType)
                {
                    int freeBlockPosition = GetPageFreeBlockIndex(pageIndex);
                    index = freeBlockPosition >= 0
                        ? freeBlockPosition
                        : index;
                }

                else if (currentPageType == TypeOfPage.EMPTY)
                {
                    pageHeader.pageType = pageType;
                    FillPage(pageIndex, pageHeader);
                    index = pageIndex + PHeader.PAGE_HEADER_SIZE;
                }

                pageIndex += PHeader.PAGE_TOTAL_SIZE;
            }

            if (index >= 0)
            {
                BHeader blockHeader = GetBlockHeader(index);
                blockHeader.IsFree = false;
                CreateBlockHeader(index, blockHeader);
            }

            return index;
        }


        public int MemRealloc(int index, int size)
        {
            if (!CheckIndex(index)) throw new IndexOutOfRangeException();

            BHeader blockHeader = GetBlockHeader(index);
            blockHeader.IsFree = true;
            CreateBlockHeader(index, blockHeader);

            byte[] data = ReadArray(index);
            int reallocIndex = MemAllocate(size);
            WriteArray(reallocIndex, data);

            return reallocIndex;
        }


        public void MemFree(int index)
        {
            if (!CheckIndex(index)) throw new IndexOutOfRangeException();

            BHeader blockHeader = GetBlockHeader(index);
            blockHeader.IsFree = true;
            CreateBlockHeader(index, blockHeader);
        }

        public String MemDump()
        {
            String dump = "";
            int index = 0;


            while (index < _size)
            {
                PHeader pageHeader = GetPageHeader(index);
                dump += pageHeader.ToString() + '\n';

                int pageDataLength = PHeader.PAGE_TOTAL_SIZE - PHeader.PAGE_HEADER_SIZE;
                byte[] pageData = new byte[pageDataLength];
                Array.Copy(_buffer, index + PHeader.PAGE_HEADER_SIZE, pageData, 0, pageDataLength);
                dump += BitConverter.ToString(pageData);
                dump += '\n';

                index += PHeader.PAGE_TOTAL_SIZE;
            }

            return dump;
        }

        public void WriteArray(int index, byte[] byteArray)
        {
            if (!CheckIndex(index)) throw new IndexOutOfRangeException();

            Array.Copy(byteArray, 0, _buffer, index + BHeader.BLOCK_HEADER_SIZE, byteArray.Length);
        }

        public byte[] ReadArray(int index)
        {
            if (!CheckIndex(index)) throw new IndexOutOfRangeException();

            int pageHeaderIndex = GetPageHeaderIndex(index);
            PHeader pageHeader = GetPageHeader(pageHeaderIndex);
            TypeOfPage pageType = pageHeader.pageType;
            int readArrayLength = pageType.Size;
            byte[] readArray = new byte[readArrayLength];
            Array.Copy(_buffer, index + BHeader.BLOCK_HEADER_SIZE, readArray, 0, readArrayLength);
            return readArray;
        }


        private class BHeader
        {
            public const int BLOCK_HEADER_SIZE = 4;
            public bool IsFree { get; set; }

            public BHeader()
            {
                this.IsFree = true;
            }

            public BHeader(byte[] byteArray)
            {
                bool _isFree = BitConverter.ToBoolean(byteArray, 0);
                this.IsFree = _isFree;
            }


            public byte[] ToByteArray()
            {
                byte[] byteArray = new byte[BLOCK_HEADER_SIZE];


                byteArray[0] = Convert.ToByte(IsFree);

                return byteArray;
            }


            public override String ToString()
            {
                return string.Format("Free: " + IsFree);
            }


        }
        private enum Type
        {
            EMPTY, // страница свободна
            BLOCK_4, // страница, разделенная на блоки и блок имеет размер 4
            BLOCK_16, // страница, разделенная на блоки и блок имеет размер 16
            BLOCK_32, // страница, разделенная на блоки и блок имеет размер 32
            BLOCK_PAGE // страница занята многостраничный блоком
        }

        private class TypeOfPage
        {


            public static readonly TypeOfPage EMPTY = new TypeOfPage(0, Type.EMPTY);
            public static readonly TypeOfPage BLOCK_4 = new TypeOfPage(4, Type.BLOCK_4);
            public static readonly TypeOfPage BLOCK_16 = new TypeOfPage(16, Type.BLOCK_16);
            public static readonly TypeOfPage BLOCK_32 = new TypeOfPage(32, Type.BLOCK_32);
            public static readonly TypeOfPage BLOCK_PAGE = new TypeOfPage(PHeader.PAGE_SIZE - BHeader.BLOCK_HEADER_SIZE, Type.BLOCK_PAGE);



            public int Size { get; private set; }
            public Type Type { get; private set; }

            public TypeOfPage(int size, Type type)
            {
                Size = size;
                Type = type;
            }

            public static IEnumerable<TypeOfPage> Values
            {
                get
                {
                    yield return EMPTY;
                    yield return BLOCK_4;
                    yield return BLOCK_16;
                    yield return BLOCK_32;
                    yield return BLOCK_PAGE;
                }
            }

            public static TypeOfPage ValueOf(int size)
            {
                TypeOfPage pageType = null;
                foreach (var el in TypeOfPage.Values)
                {
                    if (el.Size == size)
                    {
                        pageType = el;

                    }
                }
                return pageType;
            }
        }


        private class PHeader
        {
            public const int PAGE_HEADER_SIZE = 12;
            public const int PAGE_SIZE = 480;
            public const int PAGE_TOTAL_SIZE = PAGE_HEADER_SIZE + PAGE_SIZE;

            public TypeOfPage pageType { get; set; }
            private int blockNumberOfPages;
            private int blockPageIndex;

            public PHeader()
            {
                pageType = TypeOfPage.EMPTY;// по дефолту любая страница свободна
            }

            public PHeader(byte[] byteArray)
            {
                byte[] blockTypeByteArray = new byte[4];
                Array.Copy(byteArray, 0, blockTypeByteArray, 0, 4);
                byte[] blockNumberOfPagesByteArray = new byte[4];
                Array.Copy(byteArray, 4, blockNumberOfPagesByteArray, 0, 4);
                byte[] blockPageIndexByteArray = new byte[4];
                Array.Copy(byteArray, 8, blockPageIndexByteArray, 0, 4);

                this.pageType = TypeOfPage.ValueOf(BitConverter.ToInt32(blockTypeByteArray));
                this.blockNumberOfPages = BitConverter.ToInt32(blockNumberOfPagesByteArray);
                this.blockPageIndex = BitConverter.ToInt32(blockPageIndexByteArray);
            }

            public byte[] ToByteArray()
            {
                byte[] byteArray = new byte[PAGE_HEADER_SIZE];

                byte[] blockTypeArray = BitConverter.GetBytes(pageType.Size);
                byte[] blockNumberOfPagesArray = BitConverter.GetBytes(blockNumberOfPages);
                byte[] blockPageIndexArray = BitConverter.GetBytes(blockPageIndex);

                int index = 0;
                Array.Copy(blockTypeArray, 0, byteArray, index, blockTypeArray.Length);
                index = blockTypeArray.Length;
                Array.Copy(blockNumberOfPagesArray, 0, byteArray, index, blockNumberOfPagesArray.Length);
                index = blockTypeArray.Length + blockNumberOfPagesArray.Length;
                Array.Copy(blockPageIndexArray, 0, byteArray, index, blockPageIndexArray.Length);


                return byteArray;
            }


            public override String ToString()
            {
                return String.Format(
                    "Page type:  " + pageType.Type + " Block number of pages:  " + blockNumberOfPages + "Block page index:  " + blockPageIndex

                );
            }

            public static TypeOfPage GetTypeBySize(int size)
            {
                int Size = size + BHeader.BLOCK_HEADER_SIZE;
                if (Size > 32)
                {
                    return TypeOfPage.BLOCK_PAGE;
                }
                else if (Size > 16)
                {
                    return TypeOfPage.BLOCK_32;
                }
                else if (Size > 4)
                {
                    return TypeOfPage.BLOCK_16;
                }
                else
                {
                    return TypeOfPage.BLOCK_4;
                }
            }


        }
    }
}
