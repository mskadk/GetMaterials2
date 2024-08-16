using UnityEngine;

namespace Assets.Script.My
{

    public static class SpriteManager
    {
        /// <summary>
        /// 使用SpriteManager创建一个Sprite图集
        /// </summary>
        /// <param obj="spriteName">sprite文件的名字</param>
        public static GameObject Paint(GameObject obj, string spritName, int actionId, int frameId)
        {
            GameObject g = new("fw_icon");
            g.transform.position = new(0, 0, 0);
            g.transform.SetParent(obj.transform, false);
            g.layer = obj.layer;
            CatPlayerr rr = new(g);
            rr.init(spritName, true, false, true);
            rr.paintActionFrame(actionId, frameId, 0, 0);
            rr.updatePaint();
            return g;
        }


    }
}
