using UnityEngine;

namespace Assets.Script.My
{

    public static class SpriteManager
    {
        /// <summary>
        /// 生成一个Gameobject
        /// </summary>
        /// <param name="obj">生成给谁</param>
        /// <param name="spritName">sprite的名字</param>
        /// <param name="actionId">actionId</param>
        /// <param name="frameId">frameId</param>
        /// <returns></returns>
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
