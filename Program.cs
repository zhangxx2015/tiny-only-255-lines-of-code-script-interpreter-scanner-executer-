using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Commander {
    public static class ArrayExtend {
        public static T Peek<T>(this List<T> array){
            if (array.Count < 1) throw new Exception("数组为空");
            return array[0];
        }
        public static T Pop<T>(this List<T> array){
            if(array.Count<1)throw new Exception("数组为空");
            var value = array[0];
            array.RemoveAt(0);
            return value;
        }
    }
    public enum Types{
        stringToken=1,
        numberToken=2,
        boolToken=3,
        keywordToken=4,
        identToken=5,
        delimiter=6
    }
    public class Token {
        private string _value;
        public Types Type;
        public Token(string value,Types type){
            _value = value;
            Type = type;
        }
        public override string ToString(){
            return _value;
        }
    }
    public class Tokens : List<Token>{
        public override string ToString(){
            var items = new List<string>();
            foreach (var token in this){
                items.Add(string.Format("type:{0},value:{1}", token.Type,token.ToString()));
            }
            return string.Format("[{0}]", string.Join(",", items.ToArray()));
        }
    }
    /// <summary>
    /// 脚本
    /// </summary>
    public static class scripter{
        public static Dictionary<string, object> vars = new Dictionary<string, object>();
        private static object parseValue(string express) {// 只支持 数字,布尔,字符串
            if (string.IsNullOrEmpty(express)) return string.Empty;//throw new Exception("值不能为空");
            var doubleVal = 0.0;
            if (double.TryParse(express, out doubleVal)) return doubleVal;
            var intVal = 0;
            if (int.TryParse(express, out intVal)) return intVal;
            if ("TRUE" == express.ToUpper() || "FALSE" == express.ToUpper()) return bool.Parse(express);
            if (vars.ContainsKey(express)) return vars[express];
            if (express.StartsWith("\"") && express.EndsWith("\""))
                if (express.Length >= 2) express = express.Substring(1, express.Length - 2);
            return express;
        }
        public static void var(string name) {
            if (vars.ContainsKey(name)) throw new Exception(string.Format("变量已存在[{0}]", name));
            object value = null;// 不允许初始值
            vars.Add(name, value);
        }
        public static void let(string name, object value) {
            if (!vars.ContainsKey(name)) throw new Exception(string.Format("变量不存在[{0}]", name));
            vars[name] = value;
        }
        public static bool ifeq(string a, string b) {
            var valA = parseValue(a);
            var valB = parseValue(b);
            return valA.Equals(valB);
        }
        /// <summary>
        /// 词法扫描
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static Tokens Scanner(string[] lines){
            vars.Clear();
            var keywords = new[] { "var", "let", "print","println", "ifeq", "endif", "forlt", "next" };
            var tokens = new Tokens();
            for (var row = 0; row < lines.Length; row++) {
                var chars = lines[row].ToList();
                while (chars.Count > 0) {
                    var ch = chars.Peek();
                    if (' ' == ch ||
                        '\r' == ch ||
                        '\n' == ch ||
                        '\t' == ch) {
                        chars.Pop();
                        continue;
                    }
                    if ('"' == ch) {
                        chars.Pop();
                        var val = new List<char>();
                        while (chars.Count > 0) {
                            ch = chars.Peek();
                            if ('"' == ch)
                                if ('"' != val.LastOrDefault())
                                    break;
                            val.Add(chars.Pop());
                        }
                        chars.Pop();
                        var str = new string(val.ToArray());
                        tokens.Add(new Token(str, Types.stringToken));
                        continue;
                    }
                    if ("0123456789".Contains(ch)) {// 暂不支持浮点
                        var val = new List<char>();
                        while (chars.Count > 0) {
                            ch = chars.Peek();
                            if ("0123456789".Contains(ch)) {
                                val.Add(chars.Pop());
                                continue;
                            }
                            break;
                        }
                        var num = new string(val.ToArray());
                        tokens.Add(new Token(num, Types.numberToken));
                        continue;
                    }
                    var buff = new List<char>();
                    while (chars.Count > 0) {
                        ch = chars.Peek();
                        if (" \r\n".Contains(ch)) {
                            var ident = new string(buff.ToArray());
                            if (keywords.Contains(ident)) {
                                tokens.Add(new Token(ident, Types.keywordToken));
                                buff.Clear();
                                break;
                            }
                            if (new[] { "true", "false" }.Contains(ident)) {
                                tokens.Add(new Token(ident, Types.boolToken));
                                buff.Clear();
                                break;
                            }
                            tokens.Add(new Token(ident, Types.identToken));
                            buff.Clear();
                            break;
                        }
                        buff.Add(chars.Pop());
                    }
                    if (buff.Count > 0) tokens.Add(new Token(new string(buff.ToArray()), Types.identToken));
                }
            }
            return tokens;
        }
        /// <summary>
        /// 解释执行
        /// </summary>
        /// <param name="incs"></param>
        public static void Executer(Tokens incs) {
            while (incs.Count > 0) {
                var inc = incs.Pop().ToString();
                switch (inc) {
                    case "var":
                        var(incs.Pop().ToString());
                        break;
                    case "let":
                        let(incs.Pop().ToString(), parseValue(incs.Pop().ToString()));
                        break;
                    case "print":
                        Console.Write(parseValue(incs.Pop().ToString()));
                        break;
                    case "println":
                        Console.WriteLine(parseValue(incs.Pop().ToString()));
                        break;
                    case "ifeq":
                        var eq = ifeq(incs.Pop().ToString(), incs.Pop().ToString());
                        if (!eq){
                            while (incs.Count > 0) {
                                var token = incs.Peek().ToString();
                                incs.Pop();
                                if ("endif" == token)break;
                            }
                        }
                        break;
                    case "forlt":
                        var start = (double)parseValue(incs.Pop().ToString());
                        var end = (double)parseValue(incs.Pop().ToString());
                        var step = 1.0;
                        if (incs.Count > 0) step = (double)parseValue(incs.Pop().ToString());
                        var block = new Tokens();
                        for (var i = 0; i < incs.Count; i++) {
                            if ("next" == incs[i].ToString()) break;
                            block.Add(incs[i]);
                        }
                        for(;start<end;start+=step){
                            var copy = new Tokens();
                            copy.AddRange(block);
                            Executer(copy);
                        }
                        break;
                }
            }
        }
        /// <summary>
        /// 执行代码
        /// </summary>
        /// <param name="codeLines"></param>
        /// <returns></returns>
        public static int Eval(string[] codeLines){
            var ticks = Environment.TickCount;
            scripter.Executer(scripter.Scanner(codeLines));
            ticks = Environment.TickCount - ticks;
            return ticks;
        }
    }
    class Program {
        static void Main(string[] args){
            /// <summary>
            /// tiny (only 255 lines of code) script interpreter(scanner & executer) by QQ:20437023 liaisonme@hotmail.com
            /// MIT License Copyright (c) 2018 zhangxx2015
            /// 
            /// date 2018-08-17 04:12
            /// 
            /// 255 行代码的一个微型解释器, 实现了词法分析, 语法分析
            ///     变量定义        (var),
            ///     赋值            (let),
            ///     条件判断        (ifeq,endif),
            ///     循环            (foreq,next),
            ///     输出            (print,println),
            Console.WriteLine(string.Format("elapsed: {0:N} ms",scripter.Eval(new[]{
                "    // 定义变量                                  ",
                "    var a                                        ",
                "    let a true                                   ",
                "    var b                                        ",
                "    let b 1                                      ",
                "    var c                                        ",
                "    let c \"hi!\"                                ",
                "    // 输出常量及变量                            ",
                "    print 123                                    ",
                "    print \" hello from zxBasic! \"              ",
                "    println false                                ",
                "    println a                                    ",
                "    println b                                    ",
                "    println c                                    ",
                "    // 条件判断                                  ",
                "    ifeq a true                                  ",
                "        println \"a is true\"                    ",
                "    endif                                        ",
                "    // 循环执行                                  ",
                "    forlt b 3 1                                  ",
                "        println \"be happy in every day\"        ",
                "    next                                         ",
            })));
            Console.ReadLine();
        }
    }
}