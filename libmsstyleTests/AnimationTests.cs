using libmsstyle;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace libmsstyleTests
{
    [TestClass]
    public class AnimationTests
    {
        static int Padding(int size)
        {
            return ((size + 39) & -8) - 32 - size;
        }

        static byte[] BuildAnimation(params Transform[] transforms)
        {
            int size = 56;
            foreach (var t in transforms)
                size += t.StructureSize + Padding(t.StructureSize);

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(size);
                bw.Write(16); // properties index
                bw.Write(56); // transforms index
                bw.Write(0x5c74); // header padding (7989 has build-machine text here)

                bw.Write(0); // animation flags
                bw.Write(transforms.Length);
                bw.Write(0); // stagger delay
                bw.Write(0); // stagger delay cap
                bw.Write(0f); // stagger delay factor
                bw.Write(0); // z-order
                bw.Write(0); // background part
                bw.Write(0); // tuning level
                bw.Write(0f); // perspective
                bw.Write(0x625f); // properties padding

                foreach (var t in transforms)
                {
                    t.Write(bw);
                    bw.Write(new byte[Padding(t.StructureSize)]);
                }

                return ms.ToArray();
            }
        }

        [TestMethod]
        public void EarlyWin8TransformIdsUseTheLaterStructures()
        {
            var translate = new Transform
            {
                Type = TransformType.Translate3D_EarlyWin8,
                TimingFunctionID = 1,
                StartTime = 2,
                DurationTime = 3,
                Flags = TransformFlag.HasInitialValues,
                Transform3DStructure = new Transform3D
                {
                    rX = 1, rY = 2, rZ = 3,
                    rInitialX = 4, rInitialY = 5, rInitialZ = 6,
                    rOriginX = 7, rOriginY = 8, rOriginZ = 9
                }
            };
            var scale = new Transform
            {
                Type = TransformType.Scale3D_EarlyWin8,
                Transform3DStructure = new Transform3D { rX = 0.5f, rY = 0.75f, rZ = 1.5f }
            };
            var rotate = new Transform
            {
                Type = TransformType.Rotate3D_EarlyWin8,
                Transform3DStructure = new Transform3D { rInitialX = 90, rOriginY = 0.5f }
            };
            var opacity = new Transform
            {
                Type = TransformType.Opacity_EarlyWin8,
                TransformOpacity = new TransformOpacity { Opacity = 0.25f, InitialOpacity = 0.75f }
            };

            var data = BuildAnimation(translate, scale, rotate, opacity);

            int cursor = 0;
            var header = new PropertyHeader(20000, 241);
            var animation = new Animation(data, ref cursor, header);

            Assert.AreEqual(data.Length, cursor);
            Assert.AreEqual(4, animation.Transforms.Count);
            Assert.AreEqual(56, animation.Transforms[0].StructureSize);
            Assert.AreEqual(56, animation.Transforms[1].StructureSize);
            Assert.AreEqual(56, animation.Transforms[2].StructureSize);
            Assert.AreEqual(28, animation.Transforms[3].StructureSize);

            Assert.AreEqual(0x5c74, animation.HeaderPadding);
            Assert.AreEqual(0x625f, animation.PropertiesPadding);
            Assert.AreEqual(TransformType.Translate3D_EarlyWin8, animation.Transforms[0].Type);
            Assert.AreEqual(9f, animation.Transforms[0].Transform3DStructure.rOriginZ);
            Assert.AreEqual(0.5f, animation.Transforms[1].Transform3DStructure.rX);
            Assert.AreEqual(90f, animation.Transforms[2].Transform3DStructure.rInitialX);
            Assert.AreEqual(0.25f, animation.Transforms[3].TransformOpacity.Opacity);

            header.sizeInBytes = data.Length;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                animation.WriteData(bw);
                bw.Flush();
                CollectionAssert.AreEqual(data, ms.ToArray());
            }
        }
    }
}
