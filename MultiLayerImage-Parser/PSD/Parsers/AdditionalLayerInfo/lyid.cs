using System;

namespace net.rs64.MultiLayerImageParser.PSD
{
    internal static partial class AdditionalLayerInformationParser
    {
        [Serializable, AdditionalLayerInfoParser("lyid")]
        internal class lyid : AdditionalLayerInfo
        {
            public int LayerID;

            public override void ParseAddLY(SubSpanStream stream)
            {
                LayerID = stream.ReadInt32();
            }
        }

        /*
        レイヤーを名前が変更されたとしても追跡できるID指定できる追加情報らしいが、クリスタはこれを出力していないためか、多くの PSD には存在しないためIDとして使う事はできない。
        */

    }
}