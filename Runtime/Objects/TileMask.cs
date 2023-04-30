// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

namespace TilemapCreator3D {

    [System.Flags]
    public enum TileMask : int {
        None = 0,
        TopLeft = 1,
        Top = 2,
        TopRight = 4,
        Left = 8,
        Right = 16,
        BottomLeft = 32,
        Bottom = 64,
        BottomRight = 128
    };


    [System.Serializable]
    public struct TileMaskCompound {
        public enum CompoundType : int {
            Single = 0,
            Rotated = 1,
            Flip = 2,
        }

        public CompoundType Type;
        public TileMask Mask;
        public TileMask[] Variants;

        public TileMaskCompound(TileMask mask, CompoundType type) {
            Mask = mask;
            Type = type;

            if(type == CompoundType.Rotated) {
                Variants = new TileMask[3];
                Variants[0] = Mask.Rotate90();
                Variants[1] = Variants[0].Rotate90();
                Variants[2] = Variants[1].Rotate90();

            } else if(type == CompoundType.Flip) {
                Variants = new TileMask[1];
                Variants[0] = Mask.Mirror(false);

            } else {
                Variants = new TileMask[0];

            }
        }

        public int CompareMask(TileMask mask) {
            if(mask == Mask) return 0;

            switch(Type) {
                case CompoundType.Rotated:
                    for(int i = 0; i < Variants.Length; i++) {
                        if(mask == Variants[i]) return i + 1;
                    }
                    break;
                case CompoundType.Flip:
                    return mask == Variants[0] ? 2 : -1;
            }

            return -1;
        }
    }


    public static class TileMaskUtility {
            public static TileMask Rotate90(this TileMask mask) {
            return Swap(mask, TileMask.TopLeft, TileMask.TopRight) |
                   Swap(mask, TileMask.Top, TileMask.Right) |
                   Swap(mask, TileMask.TopRight, TileMask.BottomRight) |
                   Swap(mask, TileMask.Right, TileMask.Bottom) |
                   Swap(mask, TileMask.BottomRight, TileMask.BottomLeft) |
                   Swap(mask, TileMask.Bottom, TileMask.Left) |
                   Swap(mask, TileMask.BottomLeft, TileMask.TopLeft) |
                   Swap(mask, TileMask.Left, TileMask.Top);
            }

            public static TileMask Mirror(this TileMask mask, bool vertical) {
                if(vertical) {
                    return Flip(mask, TileMask.TopLeft, TileMask.BottomLeft) |
                           Flip(mask, TileMask.Top, TileMask.Bottom) |
                           Flip(mask, TileMask.TopRight, TileMask.BottomRight) |
                           Copy(mask, TileMask.Right) |
                           Copy(mask, TileMask.Left);
                } else {
                    return Flip(mask, TileMask.TopLeft, TileMask.TopRight) |
                           Flip(mask, TileMask.Left, TileMask.Right) |
                           Flip(mask, TileMask.BottomLeft, TileMask.BottomRight) |
                           Copy(mask, TileMask.Top) |
                           Copy(mask, TileMask.Bottom);
                }
            }

            private static TileMask Swap(this TileMask mask, TileMask flagA, TileMask flagB) {
                return mask.HasFlag(flagA) ? flagB : TileMask.None;
            }

            private static TileMask Flip(this TileMask mask, TileMask flagA, TileMask flagB) {
                return (mask.HasFlag(flagA) ? flagB : TileMask.None) | (mask.HasFlag(flagB) ? flagA : TileMask.None);
            }

            private static TileMask Copy(this TileMask mask, TileMask flag) {
                return mask.HasFlag(flag) ? flag : TileMask.None;
            }

    }


    public enum TileRotation : int{
        _0deg = 0,
        _90deg = 1,
        _180deg = 2,
        _270deg = 3
    }

}