using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace Assets.Scripts
{
    public class GloabalData
    {
        public static float inf = 1000000000.0f;
    }
    
    //二维坐标点
    public class node
    {
        public int x, y;
        public float score;
        public node(int xx, int yy, float s = 0) {
            x = xx;
            y = yy;
            score = s;
        }
    }

    //比较函数
    public class NodeComparer : IComparer
    {
        // Call CaseInsensitiveComparer.Compare with the parameters reversed.
        public int Compare(object x, object y)
        {
            node a = (node)x;
            node b = (node)y;
            return (int)(a.score - b.score);
        }
    }

    //棋型
    class ChessType
    {
        public int[] color;
        public int size;
    }

    //决策树结点
    class TreeNode
    {
        public int status;
        public float value;
        public node nextNode;
        public node last1;
        public node last2;
        public ArrayList wait;
        public int depth;
        public float alpha, beta;

        public void Print()
        {
            Debug.Log("TreeNode: depth:" + depth + ", status:" + status + ", value:" + value
                + ", last1:(" + last1.x + "," + last1.y + "), last2:(" + last2.x + "," + last2.y + "), nextNode:("
                + nextNode.x + "," + nextNode.y + "),    alpha = " + alpha + ", beta = " + beta);
            //Debug.Log(wait);
        }
    }


    class GameTree
    {
        public int width, height;
        int[,] chessState; //存储棋盘位置上的落子状态
        int maxDepth = 5; //决策树深度
        int range = 1; //落子备选位置范围
        public ChessType[] chessType;

        //初始化
        public GameTree(int w, int h)
        {
            width = w;
            height = h;
            chessState = new int[w, h];

            //棋型
            //chessType = new ChessType[13];
        }

        //计算下一步最佳位置
        public node Cal(int status, int[,] chess, node last1, node last2)
        {
            //复制当前棋盘状态
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    chessState[i, j] = chess[i, j];
                }
            }

            

            //如果为空棋盘
            {
                int flag = 0;
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        if (chessState[i, j] != 0) flag = 1;
                    }
                }
                if (flag == 0) return new node(7, 7);
            }

            //根节点初始化
            TreeNode root = new TreeNode();
            root.last1 = last1;
            root.last2 = last2;
            root.nextNode = new node(7, 7);
            root.status = status;
            root.value = -GloabalData.inf;
            root.depth = 0;
            root.wait = new ArrayList();
            root.alpha = -GloabalData.inf;
            root.beta = GloabalData.inf;

            //计算拓展节点
            Move(root, root.status);
            
            //决策树遍历
            Stack<TreeNode> s = new Stack<TreeNode>();
            s.Push(root);
            float return_value = -GloabalData.inf;
            node return_node = new node(7, 7);
            bool return_flag = false;
            while (s.Count > 0)
            {
                TreeNode nowNode = s.Pop();

                //棋盘更新
                if (nowNode.depth > 0) chessState[nowNode.last1.x, nowNode.last1.y] = -nowNode.status; //根节点特判

                //胜负已定
                int winner = JudgeWinner(status, nowNode.last1);
                if (winner != 0)
                {
                    return_value = GloabalData.inf * winner;
                    return_node = nowNode.last1;
                    return_flag = true;
                    if (nowNode.depth > 0) chessState[nowNode.last1.x, nowNode.last1.y] = 0;
                    nowNode.value = return_value;
                    continue;
                }
                
                //叶子节点
                if (nowNode.depth >= maxDepth)
                {
                    //printChess();
                    return_value = Evaluate(status);
                    return_node = nowNode.last1;
                    return_flag = true;
                    if (nowNode.depth > 0) chessState[nowNode.last1.x, nowNode.last1.y] = 0;
                    nowNode.value = return_value;
                    //nowNode.Print();
                    continue;
                }

                //子结点返回更新
                if (return_flag)
                {
                    //取max
                    if (nowNode.status == status)
                    {
                        if (return_value > nowNode.value)
                        {
                            nowNode.value = return_value;
                            nowNode.nextNode = return_node;
                            nowNode.alpha = nowNode.value;
                        }
                    }
                    else
                    {
                        if (return_value < nowNode.value)
                        {
                            nowNode.value = return_value;
                            nowNode.nextNode = return_node;
                            nowNode.beta = nowNode.value;
                        }
                    }
                    return_flag = false;

                    //nowNode.Print();

                    //alpha beta剪枝
                    if (nowNode.alpha >= nowNode.beta)
                    {
                        return_flag = true;
                        return_value = nowNode.value;
                        return_node = nowNode.last1;
                        if (nowNode.depth > 0) chessState[nowNode.last1.x, nowNode.last1.y] = 0;
                        while (nowNode.wait.Count > 0) nowNode.wait.RemoveAt(nowNode.wait.Count - 1);
                        continue;
                    }
                }
                

                //当前节点已经更新完成
                if (nowNode.wait.Count <= 0)
                {
                    return_node = nowNode.last1;
                    return_value = nowNode.value;
                    return_flag = true;
                    if (nowNode.depth > 0) chessState[nowNode.last1.x, nowNode.last1.y] = 0;
                    //nowNode.print();
                    continue;
                }

                //生成子结点
                TreeNode temp = new TreeNode
                {
                    last2 = nowNode.last1,
                    last1 = (node)nowNode.wait[nowNode.wait.Count - 1],
                    nextNode = new node(7, 7),
                    status = -nowNode.status,
                    depth = nowNode.depth + 1,
                    wait = new ArrayList()
                };
                nowNode.wait.RemoveAt(nowNode.wait.Count - 1);
                if (temp.status == status)
                {
                    temp.value = -GloabalData.inf;
                    temp.beta = nowNode.beta;
                    temp.alpha = -GloabalData.inf;
                }
                else
                {
                    temp.value = GloabalData.inf;
                    temp.alpha = nowNode.alpha;
                    temp.beta = GloabalData.inf;
                }

                
                //计算wait队列
                chessState[temp.last1.x, temp.last1.y] = -temp.status;
                if (temp.depth < maxDepth)
                {
                    Move(temp, temp.status);
                }
                
                s.Push(nowNode);
                s.Push(temp);
                return_flag = false;
                continue;
            }



            //返回最优选择
            return root.nextNode;
        }

        //判断是否有获胜者，是电脑返回1
        public int JudgeWinner(int status, node now)
        {
            float res = Evaluate(now);
            if (res >= 100000.0f)
            {
                if (status * chessState[now.x, now.y] > 0) return 1;
                else                                       return -1;
            }

            return 0;
        }

        public float Evaluate_old(int status)
        {
            float res = 0;
            int[,] dpRight = new int[width, height];
            int[,] dpUp= new int[width, height];
            int[,] dpRightDown = new int[width, height];
            int[,] dpRightUp = new int[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    dpRight[i, j] = dpUp[i, j] = dpRightDown[i, j] = dpRightUp[i, j] = chessState[i, j];
                }
            }

            //向右上
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (i - 1 >= 0 && dpRight[i, j] * dpRight[i - 1, j] > 0)
                    {
                        dpRight[i, j] += dpRight[i - 1, j];
                    }
                    if (j - 1 >= 0 && dpUp[i, j] * dpUp[i, j - 1] > 0)
                    {
                        dpUp[i, j] += dpUp[i, j - 1];
                    }
                    if (j - 1 >= 0 && i - 1 >= 0 && dpRightUp[i, j] * dpRightUp[i - 1, j - 1] > 0)
                    {
                        dpRightUp[i, j] += dpRightUp[i - 1, j - 1];
                    }
                }
            }

            //向右下
            for (int i = 0; i < width; i++)
            {
                for (int j = height - 1; j >= 0; j--)
                {
                    if (j + 1 < height && i - 1 >= 0 && dpRightDown[i, j] * dpRightDown[i - 1, j + 1] > 0)
                    {
                        dpRightDown[i, j] += dpRightDown[i - 1, j + 1];
                    }
                }
            }



            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (dpRight[i, j] == 5 || dpRight[i, j] == -5) return status * dpRight[i, j] > 0 ? 10000.0f : -10000.0f;
                    if (dpUp[i, j] == 5 || dpUp[i, j] == -5) return status * dpUp[i, j] > 0 ? 10000.0f : -10000.0f;
                    if (dpRightDown[i, j] == 5 || dpRightDown[i, j] == -5) return status * dpRightDown[i, j] > 0 ? 10000.0f : -10000.0f;
                    if (dpRightUp[i, j] == 5 || dpRightUp[i, j] == -5) return status * dpRightUp[i, j] > 0 ? 10000.0f : -10000.0f;
                    res += dpRight[i, j] + dpUp[i, j] + dpRightDown[i, j] + dpRightUp[i, j];
                }
            }
            if (status == -1) res = -res;
            return res;
        }

        //评估整个棋盘的局势
        public float Evaluate(int status)
        {
            float res = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    res += Evaluate(new node(i, j)) * chessState[i, j];
                }
            }

            res = res * status;
            return res;
        }

        //求单个棋子的得分，始终返回正数或0
        public float Evaluate(node now)
        {
            float res = 0;
            if (chessState[now.x, now.y] == 0) return res;

            node dir;
            int succession_l = 0;
            int succession_r = 0;
            int succession = 0;
            int block = 0;
            int block_r, block_l;

            //横向           
            succession_l = 0;
            succession_r = 0;
            succession = 1;
            block = 0;
            block_l = block_r = 0;
            dir = new node(1, 0);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_r > 0)
                            {
                                block_r++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_r++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            dir = new node(-1, 0);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_l > 0)
                            {
                                block_l++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_l++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            if (succession_r > succession_l)
            {
                res += Score(succession, succession_r, block, block_r);
            }
            else
            {
                res += Score(succession, succession_l, block, block_l);
            }

            //纵向           
            succession_l = 0;
            succession_r = 0;
            succession = 1;
            block = 0;
            block_l = block_r = 0;
            dir = new node(0, 1);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_r > 0)
                            {
                                block_r++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_r++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            dir = new node(0, -1);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_l > 0)
                            {
                                block_l++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_l++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            if (succession_r > succession_l)
            {
                res += Score(succession, succession_r, block, block_r);
            }
            else
            {
                res += Score(succession, succession_l, block, block_l);
            }

            //右上向           
            succession_l = 0;
            succession_r = 0;
            succession = 1;
            block = 0;
            block_l = block_r = 0;
            dir = new node(1, 1);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_r > 0)
                            {
                                block_r++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_r++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            dir = new node(-1, -1);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_l > 0)
                            {
                                block_l++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_l++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            if (succession_r > succession_l)
            {
                res += Score(succession, succession_r, block, block_r);
            }
            else
            {
                res += Score(succession, succession_l, block, block_l);
            }

            //右下向           
            succession_l = 0;
            succession_r = 0;
            succession = 1;
            block = 0;
            block_l = block_r = 0;
            dir = new node(1, -1);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_r > 0)
                            {
                                block_r++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_r++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            dir = new node(-1, 1);
            for (int i = 1; i <= 4; i++)
            {
                if (now.x + i * dir.x < 0 || now.x + i * dir.x >= width || now.y + i * dir.y < 0 || now.y + i * dir.y >= height || chessState[now.x + i * dir.x, now.y + i * dir.y] == -chessState[now.x, now.y])
                {
                    block++;
                    break;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == chessState[now.x, now.y])
                {
                    succession++;
                }
                else if (chessState[now.x + i * dir.x, now.y + i * dir.y] == 0)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        if (now.x + (i + j) * dir.x < 0 || now.x + (i + j) * dir.x >= width || now.y + (i + j) * dir.y < 0 || now.y + (i + j) * dir.y >= height || chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == -chessState[now.x, now.y])
                        {
                            if (succession_l > 0)
                            {
                                block_l++;
                            }
                            break;
                        }
                        else if (chessState[now.x + (i + j) * dir.x, now.y + (i + j) * dir.y] == chessState[now.x, now.y])
                        {
                            succession_l++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            if (succession_r > succession_l)
            {
                res += Score(succession, succession_r, block, block_r);
            }
            else
            {
                res += Score(succession, succession_l, block, block_l);
            }

            return res;
        }

        //计算棋型得分
        float Score(int succession, int succession2, int block, int block2)
        {
            if (succession >= 5) return 100000.0f;


            //活四
            if (succession == 4 && block == 0) return 10000.0f;

            //冲四
            if (succession == 4 && block == 1) return 1000.0f;
            if (succession2 > 0 && succession + succession2 >= 4 && block + block2 < 2) return 1000.0f;

            //活三
            if (succession == 3 && block == 0) return 1000.0f;
            if (succession2 > 0 && succession + succession2 == 3 && block + block2 == 0) return 1000.0f;

            //冲三
            if (succession == 3 && block == 1) return 100.0f;
            if (succession2 > 0 && succession + succession2 == 3 && block + block2 == 1) return 100.0f;

            //活二
            if (succession == 2 && block == 0) return 100.0f;
            if (succession2 > 0 && succession + succession2 == 2 && block + block2 == 0) return 100.0f;

            //冲二
            if (succession == 2 && block == 1) return 10.0f;
            if (succession2 > 0 && succession + succession2 == 2 && block + block2 == 1) return 10.0f;

            return 0;
        }

        //打印棋盘状态
        void printChess()
        {
            for (int j = 0; j < height; j++)
            {
                string s = "";
                for (int i = 0; i < width; i++)
                {
                    s += chessState[i, j];
                }
                Debug.Log(s);
            }
        }

        //计算拓展结点
        void Move(TreeNode node, int player)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (chessState[i, j] != 0) continue;
                    int flag = 0;
                    for (int k = -range; k <= range; k++)
                    {
                        for (int l = -range; l <= range; l++)
                        {
                            if (i + k < 0 || i + k >= width || j + l < 0 || j + l >= height) continue;
                            if (chessState[i + k, j + l] != 0)
                            {
                                flag = 1;
                                break;
                            }
                        }
                        if (flag == 1) break;
                    }

                    if (flag == 1)
                    {
                        chessState[i, j] = player;
                        float score = Evaluate(new node(i, j));
                        node.wait.Add(new node(i, j, score));
                        chessState[i, j] = 0;
                    }
                }
            }
            //根据得分对子结点排序
            node.wait.Sort(new NodeComparer());
        }
    }
}
