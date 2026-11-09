using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libmsstyle
{
    public class Transform
    {
        public TransformType Type { get; set; }
        public int TimingFunctionID { get; set; }
        public int StartTime { get; set; }
        public int DurationTime { get; set; }
        public TransformFlag Flags { get; set; }
        [TypeConverter(typeof(ExpandableObjectConverter))]

        public Transform3D Transform3DStructure { get; set; }
        [TypeConverter(typeof(ExpandableObjectConverter))]

        public Transform2D Transform2DStructure { get; set; }
        [TypeConverter(typeof(ExpandableObjectConverter))]

        public TransformOpacity TransformOpacity { get; set; }

        public int StructureSize
        {
            get
            {
                int header = 20; //header

                if (Has3DPayload(Type))
                {
                    header += 36;
                }
                else if (Has2DPayload(Type))
                {
                    header += 24;
                }
                else if (HasOpacityPayload(Type))
                {
                    header += 8;
                }
                else
                {
                    throw new NotImplementedException("Unknown transform type: " + Type);
                }

                return header;
            }
        }

        private static bool Has3DPayload(TransformType type)
        {
            return type == TransformType.Roatate3D
                || type == TransformType.Scale3D
                || type == TransformType.Translate3d
                // Windows 8 pre-release builds (e.g. 7989) used these lower ids
                // for the same 3D transform structures.
                || type == TransformType.Rotate3D_EarlyWin8
                || type == TransformType.Scale3D_EarlyWin8
                || type == TransformType.Translate3D_EarlyWin8;
        }

        private static bool Has2DPayload(TransformType type)
        {
            return type == TransformType.Translate2D || type == TransformType.Scale2D;
        }

        private static bool HasOpacityPayload(TransformType type)
        {
            return type == TransformType.Opacity
                // Same two-float opacity structure, used before the id became 2.
                || type == TransformType.Opacity_EarlyWin8;
        }
        public Transform()
        {
            Transform3DStructure = new Transform3D();
            Transform2DStructure = new Transform2D();
            TransformOpacity = new TransformOpacity();
        }

        public Transform(byte[] data, ref int start)
        {
            Type = (TransformType)BitConverter.ToInt32(data, start + 0);
            TimingFunctionID = BitConverter.ToInt32(data, start + 4);
            StartTime = BitConverter.ToInt32(data, start + 8);
            DurationTime = BitConverter.ToInt32(data, start + 12);
            Flags = (TransformFlag)BitConverter.ToInt32(data, start + 16);
            start += 20;
            if (Has3DPayload(Type))
            {
                Transform3DStructure = new Transform3D(data, start);
                start += 36;
            }
            else if (Has2DPayload(Type))
            {
                Transform2DStructure = new Transform2D(data, start);
                start += 24;
            }
            else if (HasOpacityPayload(Type))
            {
                TransformOpacity = new TransformOpacity(data, start);
                start += 8;
            }
            else
            {
                throw new NotImplementedException("Unknown transform type: " + Type);
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((int)Type);
            bw.Write(TimingFunctionID);
            bw.Write(StartTime);
            bw.Write(DurationTime);
            bw.Write((int)Flags);

            if (Has3DPayload(Type))
            {
                Transform3DStructure.Write(bw);
            }
            else if (Has2DPayload(Type))
            {
                Transform2DStructure.Write(bw);
            }
            else if (HasOpacityPayload(Type))
            {
                TransformOpacity.Write(bw);
            }
            else
            {
                throw new NotImplementedException("Unknown transform type: " + Type);
            }
        }

        public override string ToString()
        {
            return "Transform";
        }
    }

    public enum TransformType
    {
        Translate2D = 0,
        Scale2D = 1,
        Opacity = 2,
        Clip = 3,

        // Windows 8 pre-release builds (e.g. 7989) used these ids before the
        // 3D transforms moved to 258-260 and opacity moved to 2.
        Translate3D_EarlyWin8 = 4,
        Scale3D_EarlyWin8 = 5,
        Rotate3D_EarlyWin8 = 6,
        Opacity_EarlyWin8 = 8,

        Translate3d = 258,
        Scale3D = 259,
        Roatate3D = 260,
    }

    [Flags]
    public enum TransformFlag
    {
        None = 0,
        TargetValuesUser = 1,
        HasInitialValues = 2,
        HasOrginValues = 4
    }
}
