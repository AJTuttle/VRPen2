using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using System;
using UnityEngine.UI;

namespace VRPen {

    public class Calculator : MonoBehaviour {

        Stack<string> stack = new Stack<string>();
        Stack<string> bounds = new Stack<string>();
        DataTable data;

        bool onlyAnswer = false;
        bool autoClear = false;

        public Text outputText;

        private void Start() {
            data = new DataTable();
            stack.Push("");
        }

        void updateOutputText(string str) {
            outputText.text += str;
        }

        public void generalInput(string input) {


            //if its just the answer, remove if non math symbol is input
            if (onlyAnswer) {
                if (autoClear || (!input.Equals("+") && !input.Equals("-") && !input.Equals("/") && !input.Equals("*"))) {
                    clear();
                }
                onlyAnswer = false;
            }

            updateOutputText(input);

            //deal with bounds
            if (input.Equals(")")) {
                if (bounds.Peek().Equals("(")) {
                    bounds.Pop();
                }
                else {
                    computeTrig();
                    return;
                }
            }
            else if (input.Equals("(")) {
                bounds.Push("" + input);
            }
            //deal with pi or e
            else if (input.Equals("pi")) {
                input = "3.14159265";
                if (!stack.Peek().EndsWith("/") && !stack.Peek().EndsWith("*") &&
                    !stack.Peek().EndsWith("-") && !stack.Peek().EndsWith("+") &&
                    !stack.Peek().EndsWith("(") && stack.Peek().Length != 0) {
                    input = "*" + input;
                }
            }
            else if (input.Equals("e")) {
                input = "2.71828182";
                if (!stack.Peek().EndsWith("/") && !stack.Peek().EndsWith("*") &&
                    !stack.Peek().EndsWith("-") && !stack.Peek().EndsWith("+") &&
                    !stack.Peek().EndsWith("(") && stack.Peek().Length != 0) {
                    input = "*" + input;
                }
            }
            

            //add to stack
            string temp = stack.Pop();
            temp += input;
            stack.Push(temp);
        }

        public void functionInput(string input) {

            if (onlyAnswer) {
                onlyAnswer = false;
                autoClear = false;
                clear();
            }
            if (input.Equals("Compute")) {

                Debug.Log(stack.Peek() + "    ");
                //get result
                string result = "";
                try {
                    result = data.Compute(stack.Peek(), null).ToString();
                    result = roundResult(result);
                    if (bounds.Count != 0) {
                        result = "ERROR";
                        autoClear = true;
                    }
                }
                catch (Exception e) {
                    result = "ERROR";
                    autoClear = true;
                }

                //log
                List<string> logData = new List<string> {
                    outputText.text,
                    result
                };
                Logger.LogRow("calculator", logData);

                //debug
                //Debug.Log(stack.Peek() + "    ");
                //Debug.Log(data.Compute(stack.Peek(), null));

                //clear at set result into new stack
                clear();
                generalInput(result);
                onlyAnswer = true;

            }
            else if (input.Equals("Clear")) {
                clear();
            }
            else if (input.Equals("sin")) {
                stack.Push("");
                bounds.Push("sin(");
                updateOutputText("sin(");
            }
            else if (input.Equals("cos")) {
                stack.Push("");
                bounds.Push("cos(");
                updateOutputText("cos(");
            }
            else if (input.Equals("tan")) {
                stack.Push("");
                bounds.Push("tan(");
                updateOutputText("tan(");
            }
            else if (input.Equals("Asin")) {
                stack.Push("");
                bounds.Push("Asin(");
                updateOutputText("Asin(");
            }
            else if (input.Equals("Acos")) {
                stack.Push("");
                bounds.Push("Acos(");
                updateOutputText("Acos(");
            }
            else if (input.Equals("Atan")) {
                stack.Push("");
                bounds.Push("Atan(");
                updateOutputText("Atan(");
            }
        }

        private string roundResult(string result) {

            if (result.Length < 8) return result;

            float number = float.Parse(result);
            string rounded;


            if (Mathf.Abs(number) < 0.000000001) rounded = "0";
            else if (number > 99999 || number <= 0.00001f) rounded = number.ToString("E5");
            else rounded = number.ToString();

            return rounded;
            
        } 

        private void clear() {
            bounds.Clear();
            stack.Clear();
            stack.Push("");
            outputText.text = "";
        }

        public void computeTrig() {

            string function = bounds.Pop();

            if (function.Equals("sin(")) {
                string result = Mathf.Sin(Convert.ToSingle(data.Compute(stack.Pop(), null))).ToString();
                string temp = stack.Pop() + result;
                stack.Push(temp);
            }
            else if (function.Equals("cos(")) {
                string result = Mathf.Cos(Convert.ToSingle(data.Compute(stack.Pop(), null))).ToString();
                string temp = stack.Pop() + result;
                stack.Push(temp);
            }
            else if (function.Equals("tan(")) {
                string result = Mathf.Tan(Convert.ToSingle(data.Compute(stack.Pop(), null))).ToString();
                string temp = stack.Pop() + result;
                stack.Push(temp);
            }
            else if (function.Equals("Asin(")) {
                string result = Mathf.Asin(Convert.ToSingle(data.Compute(stack.Pop(), null))).ToString();
                string temp = stack.Pop() + result;
                stack.Push(temp);
            }
            else if (function.Equals("Acos(")) {
                string result = Mathf.Acos(Convert.ToSingle(data.Compute(stack.Pop(), null))).ToString();
                string temp = stack.Pop() + result;
                stack.Push(temp);
            }
            else if (function.Equals("Atan(")) {
                string result = Mathf.Atan(Convert.ToSingle(data.Compute(stack.Pop(), null))).ToString();
                string temp = stack.Pop() + result;
                stack.Push(temp);
            }

        }

    }

}
