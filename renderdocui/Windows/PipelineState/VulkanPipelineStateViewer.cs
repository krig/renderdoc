﻿/******************************************************************************
 * The MIT License (MIT)
 * 
 * Copyright (c) 2014 Crytek
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ******************************************************************************/


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using renderdocui.Code;
using renderdocui.Controls;
using renderdoc;

namespace renderdocui.Windows.PipelineState
{
    public partial class VulkanPipelineStateViewer : UserControl, ILogViewerForm
    {
        private Core m_Core;
        private DockContent m_DockContent;

        // keep track of the VB nodes (we want to be able to highlight them easily on hover)
        private List<TreelistView.Node> m_VBNodes = new List<TreelistView.Node>();
        private List<TreelistView.Node> m_BindNodes = new List<TreelistView.Node>();

        public VulkanPipelineStateViewer(Core core, DockContent c)
        {
            InitializeComponent();

            m_DockContent = c;

            viAttrs.Font = core.Config.PreferredFont;
            viBuffers.Font = core.Config.PreferredFont;

            csUAVs.Font = core.Config.PreferredFont;
            gsStreams.Font = core.Config.PreferredFont;

            groupX.Font = groupY.Font = groupZ.Font = core.Config.PreferredFont;
            threadX.Font = threadY.Font = threadZ.Font = core.Config.PreferredFont;

            vsShader.Font = vsResources.Font = vsSamplers.Font = vsCBuffers.Font = vsClasses.Font = core.Config.PreferredFont;
            gsShader.Font = gsResources.Font = gsSamplers.Font = gsCBuffers.Font = gsClasses.Font = core.Config.PreferredFont;
            hsShader.Font = hsResources.Font = hsSamplers.Font = hsCBuffers.Font = hsClasses.Font = core.Config.PreferredFont;
            dsShader.Font = dsResources.Font = dsSamplers.Font = dsCBuffers.Font = dsClasses.Font = core.Config.PreferredFont;
            psShader.Font = psResources.Font = psSamplers.Font = psCBuffers.Font = psClasses.Font = core.Config.PreferredFont;
            csShader.Font = csResources.Font = csSamplers.Font = csCBuffers.Font = csClasses.Font = core.Config.PreferredFont;

            viewports.Font = core.Config.PreferredFont;
            scissors.Font = core.Config.PreferredFont;

            targetOutputs.Font = core.Config.PreferredFont;
            blendOperations.Font = core.Config.PreferredFont;
            
            pipeFlow.Font = new System.Drawing.Font(core.Config.PreferredFont.FontFamily, 11.25F,
                System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            pipeFlow.SetStages(new KeyValuePair<string, string>[] {
                new KeyValuePair<string,string>("IA", "Input Assembler"),
                new KeyValuePair<string,string>("VS", "Vertex Shader"),
                new KeyValuePair<string,string>("HS", "Hull Shader"),
                new KeyValuePair<string,string>("DS", "Domain Shader"),
                new KeyValuePair<string,string>("GS", "Geometry Shader"),
                new KeyValuePair<string,string>("RS", "Rasterizer"),
                new KeyValuePair<string,string>("PS", "Pixel Shader"),
                new KeyValuePair<string,string>("OM", "Output Merger"),
                new KeyValuePair<string,string>("CS", "Compute Shader"),
            });

            pipeFlow.IsolateStage(8); // compute shader isolated

            pipeFlow.SetStagesEnabled(new bool[] { true, true, true, true, true, true, true, true, true });

            //Icon = global::renderdocui.Properties.Resources.icon;

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            toolTip.SetToolTip(vsShaderCog, "Open Shader Source");
            toolTip.SetToolTip(dsShaderCog, "Open Shader Source");
            toolTip.SetToolTip(hsShaderCog, "Open Shader Source");
            toolTip.SetToolTip(gsShaderCog, "Open Shader Source");
            toolTip.SetToolTip(psShaderCog, "Open Shader Source");
            toolTip.SetToolTip(csShaderCog, "Open Shader Source");

            toolTip.SetToolTip(vsShader, "Open Shader Source");
            toolTip.SetToolTip(dsShader, "Open Shader Source");
            toolTip.SetToolTip(hsShader, "Open Shader Source");
            toolTip.SetToolTip(gsShader, "Open Shader Source");
            toolTip.SetToolTip(psShader, "Open Shader Source");
            toolTip.SetToolTip(csShader, "Open Shader Source");

            OnLogfileClosed();

            m_Core = core;
        }

        public void OnLogfileClosed()
        {
            viAttrs.Nodes.Clear();
            viBuffers.Nodes.Clear();
            topology.Text = "";
            topologyDiagram.Image = null;

            ClearShaderState(vsShader, vsResources, vsSamplers, vsCBuffers, vsClasses);
            ClearShaderState(gsShader, gsResources, gsSamplers, gsCBuffers, gsClasses);
            ClearShaderState(hsShader, hsResources, hsSamplers, hsCBuffers, hsClasses);
            ClearShaderState(dsShader, dsResources, dsSamplers, dsCBuffers, dsClasses);
            ClearShaderState(psShader, psResources, psSamplers, psCBuffers, psClasses);
            ClearShaderState(csShader, csResources, csSamplers, csCBuffers, csClasses);

            csUAVs.Nodes.Clear();
            gsStreams.Nodes.Clear();

            var tick = global::renderdocui.Properties.Resources.tick;

            fillMode.Text = "Solid";
            cullMode.Text = "Front";
            frontCCW.Image = tick;

            scissorEnable.Image = tick;
            lineAAEnable.Image = tick;
            multisampleEnable.Image = tick;

            depthClip.Image = tick;
            depthBias.Text = "0";
            depthBiasClamp.Text = "0.0";
            slopeScaledBias.Text = "0.0";

            viewports.Nodes.Clear();
            scissors.Nodes.Clear();

            targetOutputs.Nodes.Clear();
            blendOperations.Nodes.Clear();

            alphaToCoverage.Image = tick;
            independentBlend.Image = tick;

            blendFactor.Text = "0.00, 0.00, 0.00, 0.00";

            sampleMask.Text = "FFFFFFFF";

            depthEnable.Image = tick;
            depthFunc.Text = "GREATER_EQUAL";
            depthWrite.Image = tick;

            stencilEnable.Image = tick;
            stencilReadMask.Text = "FF";
            stencilWriteMask.Text = "FF";
            stencilRef.Text = "FF";

            stencilFuncs.Nodes.Clear();

            pipeFlow.SetStagesEnabled(new bool[] { true, true, true, true, true, true, true, true, true });
        }
        
        public void OnLogfileLoaded()
        {
            OnEventSelected(m_Core.CurFrame, m_Core.CurEvent);
        }

        private void EmptyRow(TreelistView.Node node)
        {
            node.BackColor = Color.Firebrick;
        }

        private void InactiveRow(TreelistView.Node node)
        {
            node.Italic = true;
        }

        private void ClearShaderState(Label shader, TreelistView.TreeListView resources, TreelistView.TreeListView samplers,
                                      TreelistView.TreeListView cbuffers, TreelistView.TreeListView classes)
        {
            shader.Text = "Unbound";
            resources.Nodes.Clear();
            samplers.Nodes.Clear();
            cbuffers.Nodes.Clear();
            classes.Nodes.Clear();
        }

        // Set a shader stage's resources and values
        private void SetShaderState(FetchTexture[] texs, FetchBuffer[] bufs,
                                    VulkanPipelineState.ShaderStage stage,
                                    Label shader, TreelistView.TreeListView resources, TreelistView.TreeListView samplers,
                                    TreelistView.TreeListView cbuffers, TreelistView.TreeListView classes)
        {
            ShaderReflection shaderDetails = stage.ShaderDetails;

            if (stage.Shader == ResourceId.Null)
                shader.Text = "Unbound";
            else
                shader.Text = stage.ShaderName;

            if (shaderDetails != null && shaderDetails.DebugInfo.entryFunc.Length > 0 && shaderDetails.DebugInfo.files.Length > 0)
            {
                string shaderfn = "";

                int entryFile = shaderDetails.DebugInfo.entryFile;
                if (entryFile < 0 || entryFile >= shaderDetails.DebugInfo.files.Length)
                    entryFile = 0;

                shaderfn = shaderDetails.DebugInfo.files[entryFile].BaseFilename;

                shader.Text = shaderDetails.DebugInfo.entryFunc + "()" + " - " + shaderfn;
            }

            int vs = 0;
            
            vs = resources.VScrollValue();
            resources.BeginUpdate();
            resources.Nodes.Clear();

            resources.EndUpdate();
            resources.NodesSelection.Clear();
            resources.SetVScrollValue(vs);

            vs = samplers.VScrollValue();
            samplers.BeginUpdate();
            samplers.Nodes.Clear();

            samplers.EndUpdate();
            samplers.NodesSelection.Clear();
            samplers.SetVScrollValue(vs);

            vs = cbuffers.VScrollValue();
            cbuffers.BeginUpdate();
            cbuffers.Nodes.Clear();

            cbuffers.EndUpdate();
            cbuffers.NodesSelection.Clear();
            cbuffers.SetVScrollValue(vs);

            vs = classes.VScrollValue();
            classes.BeginUpdate();
            classes.Nodes.Clear();

            classes.EndUpdate();
            classes.NodesSelection.Clear();
            classes.SetVScrollValue(vs);

            classes.Visible = classes.Parent.Visible = false;
        }

        // from https://gist.github.com/mjijackson/5311256
        private float CalcHue(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;

            if (t < 1.0f / 6.0f)
                return p + (q - p) * 6.0f * t;

            if (t < 0.5f)
                return q;

            if (t < 2.0f / 3.0f)
                return p + (q - p) * (2.0f / 3.0f - t) * 6.0f;

            return p;
        }

        private Color HSLColor(float h, float s, float l)
        {
            float r, g, b;

            if (s == 0)
            {
                r = g = b = l; // achromatic
            }
            else
            {
                var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                var p = 2 * l - q;
                r = CalcHue(p, q, h + 1.0f / 3.0f);
                g = CalcHue(p, q, h);
                b = CalcHue(p, q, h - 1.0f / 3.0f);
            }

            return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private void UpdateState()
        {
            if (!m_Core.LogLoaded)
                return;

            FetchTexture[] texs = m_Core.CurTextures;
            FetchBuffer[] bufs = m_Core.CurBuffers;
            VulkanPipelineState state = m_Core.CurVulkanPipelineState;
            FetchDrawcall draw = m_Core.CurDrawcall;

            var tick = global::renderdocui.Properties.Resources.tick;
            var cross = global::renderdocui.Properties.Resources.cross;

            bool[] usedBindings = new bool[128];
            bool[] usedVBuffers = new bool[128];

            for (int i = 0; i < 128; i++)
                usedBindings[i] = usedVBuffers[i] = false;

            ////////////////////////////////////////////////
            // Vertex Input

            int vs = 0;
            
            vs = viAttrs.VScrollValue();
            viAttrs.Nodes.Clear();
            viAttrs.BeginUpdate();
            if (state.VI.attrs != null)
            {
                int i = 0;
                foreach (var a in state.VI.attrs)
                {
                    bool filledSlot = true;
                    bool usedSlot = false;

                    string name = String.Format("Attribute {0}", i);

                    if (state.VS.Shader != ResourceId.Null)
                    {
                        int attrib = state.VS.BindpointMapping.InputAttributes[a.location];

                        if (attrib >= 0 && attrib < state.VS.ShaderDetails.InputSig.Length)
                        {
                            name = state.VS.ShaderDetails.InputSig[attrib].varName;
                            usedSlot = true;
                        }
                    }
                    
                    // show if
                    if (usedSlot || // it's referenced by the shader - regardless of empty or not
                        (showDisabled.Checked && !usedSlot && filledSlot) || // it's bound, but not referenced, and we have "show disabled"
                        (showEmpty.Checked && !filledSlot) // it's empty, and we have "show empty"
                        )
                    {
                        var node = viAttrs.Nodes.Add(new object[] {
                                              i, name, a.location, a.binding, a.format, a.byteoffset });

                        usedBindings[a.binding] = true;

                        node.Image = global::renderdocui.Properties.Resources.action;
                        node.HoverImage = global::renderdocui.Properties.Resources.action_hover;

                        if (!usedSlot)
                            InactiveRow(node);
                    }

                    i++;
                }
            }
            viAttrs.NodesSelection.Clear();
            viAttrs.EndUpdate();
            viAttrs.SetVScrollValue(vs);

            m_BindNodes.Clear();

            vs = viBindings.VScrollValue();
            viBindings.Nodes.Clear();
            viBindings.BeginUpdate();
            if (state.VI.binds != null)
            {
                foreach (var a in state.VI.binds)
                {
                    if (showDisabled.Checked || usedBindings[a.vbufferBinding])
                    {
                        var node = viBindings.Nodes.Add(new object[] { a.vbufferBinding, a.bytestride, a.perInstance ? "Instance" : "Vertex" });

                        usedVBuffers[a.vbufferBinding] = true;

                        node.Image = global::renderdocui.Properties.Resources.action;
                        node.HoverImage = global::renderdocui.Properties.Resources.action_hover;

                        if (!usedBindings[a.vbufferBinding])
                            InactiveRow(node);

                        m_BindNodes.Add(node);
                    }
                }
            }
            viBindings.NodesSelection.Clear();
            viBindings.EndUpdate();
            viBindings.SetVScrollValue(vs);

            PrimitiveTopology topo = draw != null ? draw.topology : PrimitiveTopology.Unknown;

            topology.Text = topo.ToString();
            if (topo > PrimitiveTopology.PatchList)
            {
                int numCPs = (int)topo - (int)PrimitiveTopology.PatchList + 1;

                topology.Text = string.Format("PatchList ({0} Control Points)", numCPs);
            }

            switch (topo)
            {
                case PrimitiveTopology.PointList:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_pointlist;
                    break;
                case PrimitiveTopology.LineList:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_linelist;
                    break;
                case PrimitiveTopology.LineStrip:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_linestrip;
                    break;
                case PrimitiveTopology.TriangleList:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_trilist;
                    break;
                case PrimitiveTopology.TriangleStrip:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_tristrip;
                    break;
                case PrimitiveTopology.LineList_Adj:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_linelist_adj;
                    break;
                case PrimitiveTopology.LineStrip_Adj:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_linestrip_adj;
                    break;
                case PrimitiveTopology.TriangleList_Adj:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_trilist_adj;
                    break;
                case PrimitiveTopology.TriangleStrip_Adj:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_tristrip_adj;
                    break;
                default:
                    topologyDiagram.Image = global::renderdocui.Properties.Resources.topo_patch;
                    break;
            }

            vs = viBuffers.VScrollValue();
            viBuffers.Nodes.Clear();
            viBuffers.BeginUpdate();

            bool ibufferUsed = draw != null && (draw.flags & DrawcallFlags.UseIBuffer) != 0;

            if (state.IA.ibuffer != null && state.IA.ibuffer.buf != ResourceId.Null)
            {
                if (ibufferUsed || showDisabled.Checked)
                {
                    string ptr = "Buffer " + state.IA.ibuffer.buf.ToString();
                    string name = ptr;
                    UInt32 length = 1;

                    if (!ibufferUsed)
                    {
                        length = 0;
                    }

                    for (int t = 0; t < bufs.Length; t++)
                    {
                        if (bufs[t].ID == state.IA.ibuffer.buf)
                        {
                            name = bufs[t].name;
                            length = bufs[t].length;
                        }
                    }

                    var node = viBuffers.Nodes.Add(new object[] { "Index", name, state.IA.ibuffer.offs, length });

                    node.Image = global::renderdocui.Properties.Resources.action;
                    node.HoverImage = global::renderdocui.Properties.Resources.action_hover;
                    node.Tag = state.IA.ibuffer.buf;

                    if (!ibufferUsed)
                        InactiveRow(node);

                    if (state.IA.ibuffer.buf == ResourceId.Null)
                        EmptyRow(node);
                }
            }
            else
            {
                if (ibufferUsed || showEmpty.Checked)
                {
                    var node = viBuffers.Nodes.Add(new object[] { "Index", "No Buffer Set", "-", "-" });

                    node.Image = global::renderdocui.Properties.Resources.action;
                    node.HoverImage = global::renderdocui.Properties.Resources.action_hover;
                    node.Tag = state.IA.ibuffer.buf;

                    EmptyRow(node);

                    if (!ibufferUsed)
                        InactiveRow(node);
                }
            }

            m_VBNodes.Clear();

            if (state.VI.vbuffers != null)
            {
                int i = 0;
                foreach (var v in state.VI.vbuffers)
                {
                    bool filledSlot = (v.buffer != ResourceId.Null);
                    bool usedSlot = (usedVBuffers[i]);

                    // show if
                    if (usedSlot || // it's referenced by the shader - regardless of empty or not
                        (showDisabled.Checked && !usedSlot && filledSlot) || // it's bound, but not referenced, and we have "show disabled"
                        (showEmpty.Checked && !filledSlot) // it's empty, and we have "show empty"
                        )
                    {
                        string name = "Buffer " + v.buffer.ToString();
                        UInt32 length = 1;

                        for (int t = 0; t < bufs.Length; t++)
                        {
                            if (bufs[t].ID == v.buffer)
                            {
                                name = bufs[t].name;
                                length = bufs[t].length;
                            }
                        }

                        TreelistView.Node node = null;
                        
                        if(filledSlot)
                            node = viBuffers.Nodes.Add(new object[] { i, name, v.offset, length });
                        else
                            node = viBuffers.Nodes.Add(new object[] { i, "No Buffer Set", "-", "-" });

                        node.Image = global::renderdocui.Properties.Resources.action;
                        node.HoverImage = global::renderdocui.Properties.Resources.action_hover;
                        node.Tag = v.buffer;

                        if (!filledSlot)
                            EmptyRow(node);

                        if (!usedSlot)
                            InactiveRow(node);

                        m_VBNodes.Add(node);
                    }

                    i++;
                }
            }
            viBuffers.NodesSelection.Clear();
            viBuffers.EndUpdate();
            viBuffers.SetVScrollValue(vs);

            SetShaderState(texs, bufs, state.VS, vsShader, vsResources, vsSamplers, vsCBuffers, vsClasses);
            SetShaderState(texs, bufs, state.GS, gsShader, gsResources, gsSamplers, gsCBuffers, gsClasses);
            SetShaderState(texs, bufs, state.TCS, hsShader, hsResources, hsSamplers, hsCBuffers, hsClasses);
            SetShaderState(texs, bufs, state.TES, dsShader, dsResources, dsSamplers, dsCBuffers, dsClasses);
            SetShaderState(texs, bufs, state.FS, psShader, psResources, psSamplers, psCBuffers, psClasses);
            SetShaderState(texs, bufs, state.CS, csShader, csResources, csSamplers, csCBuffers, csClasses);

            vs = csUAVs.VScrollValue();
            csUAVs.Nodes.Clear();
            csUAVs.BeginUpdate();

            csUAVs.NodesSelection.Clear();
            csUAVs.EndUpdate();
            csUAVs.SetVScrollValue(vs);

            bool streamoutSet = false;
            vs = gsStreams.VScrollValue();
            gsStreams.BeginUpdate();
            gsStreams.Nodes.Clear();

            gsStreams.EndUpdate();
            gsStreams.NodesSelection.Clear();
            gsStreams.SetVScrollValue(vs);

            gsStreams.Visible = gsStreams.Parent.Visible = streamoutSet;
            if (streamoutSet)
                geomTableLayout.ColumnStyles[1].Width = 50.0f;
            else
                geomTableLayout.ColumnStyles[1].Width = 0;

            ////////////////////////////////////////////////
            // Rasterizer

            vs = viewports.VScrollValue();
            viewports.BeginUpdate();
            viewports.Nodes.Clear();

            int vs2 = scissors.VScrollValue();
            scissors.BeginUpdate();
            scissors.Nodes.Clear();
            {
                int i = 0;
                foreach (var v in state.VP.viewportScissors)
                {
                    var node = viewports.Nodes.Add(new object[] { i, v.vp.x, v.vp.y, v.vp.Width, v.vp.Height, v.vp.MinDepth, v.vp.MaxDepth });

                    if (v.vp.Width == 0 || v.vp.Height == 0 || v.vp.MinDepth == v.vp.MaxDepth)
                        EmptyRow(node);

                    i++;

                    node = scissors.Nodes.Add(new object[] { i, v.scissor.x, v.scissor.y, v.scissor.right - v.scissor.x, v.scissor.bottom - v.scissor.y });

                    if (v.scissor.right == v.scissor.x || v.scissor.bottom == v.scissor.y)
                        EmptyRow(node);
                }
            }

            viewports.NodesSelection.Clear();
            viewports.EndUpdate();
            viewports.SetVScrollValue(vs);

            scissors.NodesSelection.Clear();
            scissors.EndUpdate();
            scissors.SetVScrollValue(vs2);

            fillMode.Text = state.RS.FillMode.ToString();
            cullMode.Text = state.RS.CullMode.ToString();
            frontCCW.Image = state.RS.FrontCCW ? tick : cross;

            // VKTODOLOW update these states
            scissorEnable.Image = tick;
            lineAAEnable.Image = tick;
            multisampleEnable.Image = tick;

            depthClip.Image = state.RS.depthClipEnable ? tick : cross;
            depthBias.Text = state.RS.depthBias.ToString();
            depthBiasClamp.Text = Formatter.Format(state.RS.depthBiasClamp);
            slopeScaledBias.Text = Formatter.Format(state.RS.slopeScaledDepthBias);
            forcedSampleCount.Text = "1";

            ////////////////////////////////////////////////
            // Output Merger

            bool[] targets = new bool[8];

            for (int i = 0; i < 8; i++)
                targets[i] = false;

            vs = targetOutputs.VScrollValue();
            targetOutputs.BeginUpdate();
            targetOutputs.Nodes.Clear();
            if (state.Pass.framebuffer.attachments != null)
            {
                int i = 0;
                foreach (var p in state.Pass.framebuffer.attachments)
                {
                    if (p.img != ResourceId.Null || showEmpty.Checked)
                    {
                        UInt32 w = 1, h = 1, d = 1;
                        UInt32 a = 1;
                        string format = "Unknown";
                        string name = "Texture " + p.ToString();
                        string typename = "Unknown";
                        object tag = null;

                        if (p.img == ResourceId.Null)
                        {
                            name = "Empty";
                            format = "-";
                            typename = "-";
                            w = h = d = a = 0;
                        }

                        for (int t = 0; t < texs.Length; t++)
                        {
                            if (texs[t].ID == p.img)
                            {
                                w = texs[t].width;
                                h = texs[t].height;
                                d = texs[t].depth;
                                a = texs[t].arraysize;
                                format = texs[t].format.ToString();
                                name = texs[t].name;
                                typename = texs[t].resType.Str();

                                tag = texs[t];
                            }
                        }

                        var node = targetOutputs.Nodes.Add(new object[] { i, name, typename, w, h, d, a, format });

                        node.Image = global::renderdocui.Properties.Resources.action;
                        node.HoverImage = global::renderdocui.Properties.Resources.action_hover;
                        node.Tag = tag;

                        if (p.img == ResourceId.Null)
                        {
                            EmptyRow(node);
                        }
                        else
                        {
                            targets[i] = true;
                        }
                    }

                    i++;
                }
            }

            targetOutputs.EndUpdate();
            targetOutputs.NodesSelection.Clear();
            targetOutputs.SetVScrollValue(vs);

            vs = blendOperations.VScrollValue();
            blendOperations.BeginUpdate();
            blendOperations.Nodes.Clear();
            {
                int i = 0;
                foreach(var blend in state.CB.attachments)
                {
                    bool filledSlot = true;
                    bool usedSlot = (targets[i]);

                    // show if
                    if (usedSlot || // it's referenced by the shader - regardless of empty or not
                        (showDisabled.Checked && !usedSlot && filledSlot) || // it's bound, but not referenced, and we have "show disabled"
                        (showEmpty.Checked && !filledSlot) // it's empty, and we have "show empty"
                        )
                    {
                        var node = blendOperations.Nodes.Add(new object[] { i,
                                                        true,
                                                        state.CB.logicOpEnable,

                                                        blend.m_Blend.Source,
                                                        blend.m_Blend.Destination,
                                                        blend.m_Blend.Operation,

                                                        blend.m_AlphaBlend.Source,
                                                        blend.m_AlphaBlend.Destination,
                                                        blend.m_AlphaBlend.Operation,

                                                        state.CB.LogicOp,

                                                        ((blend.WriteMask & 0x1) == 0 ? "_" : "R") +
                                                        ((blend.WriteMask & 0x2) == 0 ? "_" : "G") +
                                                        ((blend.WriteMask & 0x4) == 0 ? "_" : "B") +
                                                        ((blend.WriteMask & 0x8) == 0 ? "_" : "A")
                    });


                        if (!filledSlot)
                            EmptyRow(node);

                        if (!usedSlot)
                            InactiveRow(node);
                    }

                    i++;
                }
            }
            blendOperations.NodesSelection.Clear();
            blendOperations.EndUpdate();
            blendOperations.SetVScrollValue(vs);


            alphaToCoverage.Image = state.CB.alphaToCoverageEnable ? tick : cross;
            independentBlend.Image = tick;

            blendFactor.Text = state.CB.blendConst[0].ToString("F2") + ", " +
                                state.CB.blendConst[1].ToString("F2") + ", " +
                                state.CB.blendConst[2].ToString("F2") + ", " +
                                state.CB.blendConst[3].ToString("F2");

            sampleMask.Text = state.MSAA.sampleMask.ToString("X8");

            depthEnable.Image = state.DS.depthTestEnable ? tick : cross;
            depthFunc.Text = state.DS.depthCompareOp;
            depthWrite.Image = state.DS.depthWriteEnable ? tick : cross;

            stencilEnable.Image = state.DS.stencilTestEnable ? tick : cross;
            stencilReadMask.Text = state.DS.StencilReadMask.ToString("X2");
            stencilWriteMask.Text = state.DS.StencilWriteMask.ToString("X2");
            stencilRef.Text = state.DS.front.stencilref.ToString("X2");

            stencilFuncs.BeginUpdate();
            stencilFuncs.Nodes.Clear();
            stencilFuncs.Nodes.Add(new object[] { "Front", state.DS.front.func, state.DS.front.failOp, 
                                                 state.DS.front.depthFailOp, state.DS.front.passOp });
            stencilFuncs.Nodes.Add(new object[] { "Back", state.DS.back.func, state.DS.back.failOp, 
                                                 state.DS.back.depthFailOp, state.DS.back.passOp });
            stencilFuncs.EndUpdate();
            stencilFuncs.NodesSelection.Clear();

            // highlight the appropriate stages in the flowchart
            if (draw == null)
            {
                pipeFlow.SetStagesEnabled(new bool[] { true, true, true, true, true, true, true, true, true });
            }
            else if ((draw.flags & DrawcallFlags.Dispatch) != 0)
            {
                pipeFlow.SetStagesEnabled(new bool[] { false, false, false, false, false, false, false, false, true });
            }
            else
            {
                pipeFlow.SetStagesEnabled(new bool[] {
                    true,
                    true,
                    state.TCS.Shader != ResourceId.Null,
                    state.TES.Shader != ResourceId.Null,
                    state.GS.Shader != ResourceId.Null,
                    true,
                    state.FS.Shader != ResourceId.Null,
                    true,
                    false
                });

                // if(streamout only)
                //{
                //    pipeFlow.Rasterizer = false;
                //    pipeFlow.OutputMerger = false;
                //}
            }
        }

        public void OnEventSelected(UInt32 frameID, UInt32 eventID)
        {
            UpdateState();
        }

        private void hideDisabledEmpty_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                rightclickMenu.Show((Control)sender, new Point(e.X, e.Y));
            }
        }

        private void hideDisabled_Click(object sender, EventArgs e)
        {
            showDisabled.Checked = !showDisabled.Checked;
            showDisabledToolitem.Checked = showDisabled.Checked;
            UpdateState();
        }

        private void hideEmpty_Click(object sender, EventArgs e)
        {
            showEmpty.Checked = !showEmpty.Checked;
            showEmptyToolitem.Checked = showEmpty.Checked;
            UpdateState();
        }

        // launch the appropriate kind of viewer, depending on the type of resource that's in this node
        private void textureCell_CellDoubleClick(TreelistView.Node node)
        {
        }

        private void defaultCopyPaste_KeyDown(object sender, KeyEventArgs e)
        {
            if (!m_Core.LogLoaded) return;

            if (e.KeyCode == Keys.C && e.Control)
            {
                string text = "";

                if (sender is DataGridView)
                {
                    foreach (DataGridViewRow row in ((DataGridView)sender).SelectedRows)
                    {
                        foreach (var cell in row.Cells)
                            text += cell.ToString() + " ";
                        text += Environment.NewLine;
                    }
                }
                else if (sender is TreelistView.TreeListView)
                {
                    TreelistView.NodesSelection sel = ((TreelistView.TreeListView)sender).NodesSelection;

                    if (sel.Count > 0)
                    {
                        for (int i = 0; i < sel.Count; i++)
                        {
                            for (int v = 0; v < sel[i].Count; v++)
                                text += sel[i][v].ToString() + " ";
                            text += Environment.NewLine;
                        }
                    }
                    else
                    {
                        TreelistView.Node n = ((TreelistView.TreeListView)sender).SelectedNode;
                        for (int v = 0; v < n.Count; v++)
                            text += n[v].ToString() + " ";
                        text += Environment.NewLine;
                    }
                }

                try
                {
                    if (text.Length > 0)
                        Clipboard.SetText(text);
                }
                catch (System.Exception)
                {
                    try
                    {
                        if (text.Length > 0)
                            Clipboard.SetDataObject(text);
                    }
                    catch (System.Exception)
                    {
                        // give up!
                    }
                }
            }
        }

        private void disableSelection_Leave(object sender, EventArgs e)
        {
            if (sender is DataGridView)
                ((DataGridView)sender).ClearSelection();
            else if (sender is TreelistView.TreeListView)
            {
                TreelistView.TreeListView tv = (TreelistView.TreeListView)sender;

                int vs = tv.VScrollValue();
                tv.NodesSelection.Clear();
                tv.FocusedNode = null;
                tv.SetVScrollValue(vs);
            }
        }

        private void disableSelection_VisibleChanged(object sender, EventArgs e)
        {
            ((DataGridView)sender).ClearSelection();
            ((DataGridView)sender).AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void iabuffers_NodeDoubleClicked(TreelistView.Node node)
        {
            if (node.Tag is ResourceId)
            {
                ResourceId id = (ResourceId)node.Tag;

                if (id != ResourceId.Null)
                {
                    var viewer = new BufferViewer(m_Core, false);
                    viewer.ViewRawBuffer(true, id);
                    viewer.Show(m_DockContent.DockPanel);
                }
            }
        }

        private void inputLayouts_NodeDoubleClick(TreelistView.Node node)
        {
            (new BufferViewer(m_Core, true)).Show(m_DockContent.DockPanel);
        }

        private VulkanPipelineState.ShaderStage GetStageForSender(object sender)
        {
            VulkanPipelineState.ShaderStage stage = null;

            if (!m_Core.LogLoaded)
                return null;

            object cur = sender;

            while (cur is Control)
            {
                if (cur == tabVS)
                    stage = m_Core.CurVulkanPipelineState.VS;
                else if (cur == tabGS)
                    stage = m_Core.CurVulkanPipelineState.GS;
                else if (cur == tabHS)
                    stage = m_Core.CurVulkanPipelineState.TCS;
                else if (cur == tabDS)
                    stage = m_Core.CurVulkanPipelineState.TES;
                else if (cur == tabPS)
                    stage = m_Core.CurVulkanPipelineState.FS;
                else if (cur == tabCS)
                    stage = m_Core.CurVulkanPipelineState.CS;
                else if (cur == tabOM)
                    stage = m_Core.CurVulkanPipelineState.FS;

                if (stage != null)
                    return stage;

                Control c = (Control)cur;

                if(c.Parent == null)
                    break;

                cur = ((Control)cur).Parent;
            }

            System.Diagnostics.Debug.Fail("Unrecognised control calling event handler");

            return null;
        }

        private void shader_Click(object sender, EventArgs e)
        {
            VulkanPipelineState.ShaderStage stage = GetStageForSender(sender);

            if (stage == null) return;

            ShaderReflection shaderDetails = stage.ShaderDetails;

            if (stage.Shader == ResourceId.Null) return;

            ShaderViewer s = new ShaderViewer(m_Core, shaderDetails, stage.stage, null, "");

            s.Show(m_DockContent.DockPanel);
        }

        private void MakeShaderVariablesHLSL(bool cbufferContents, ShaderConstant[] vars, ref string struct_contents, ref string struct_defs)
        {
            var nl = Environment.NewLine;
            var nl2 = Environment.NewLine + Environment.NewLine;

            foreach (var v in vars)
            {
                if (v.type.members.Length > 0)
                {
                    string def = "struct " + v.type.descriptor.name + " {" + nl;

                    if(!struct_defs.Contains(def))
                    {
                        string contents = "";
                        MakeShaderVariablesHLSL(false, v.type.members, ref contents, ref struct_defs);

                        struct_defs += def + contents + "};" + nl2;
                    }
                }

                struct_contents += "\t" + v.type.descriptor.name + " " + v.name;

                char comp = 'x';
                if (v.reg.comp == 1) comp = 'y';
                if (v.reg.comp == 2) comp = 'z';
                if (v.reg.comp == 3) comp = 'w';

                if (cbufferContents) struct_contents += String.Format(" : packoffset(c{0}.{1});", v.reg.vec, comp);
                else struct_contents += ";";

                struct_contents += nl;
            }
        }

        // start a shaderviewer to edit this shader, optionally generating stub HLSL if there isn't
        // HLSL source available for this shader.
        private void shaderedit_Click(object sender, EventArgs e)
        {
            VulkanPipelineState.ShaderStage stage = GetStageForSender(sender);

            if (stage == null) return;

            ShaderReflection shaderDetails = stage.ShaderDetails;

            if (stage.Shader == ResourceId.Null || shaderDetails == null) return;

            var entryFunc = String.Format("EditedShader{0}S", stage.stage.ToString()[0]);

            string mainfile = "";

            var files = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (shaderDetails.DebugInfo.entryFunc.Length > 0 && shaderDetails.DebugInfo.files.Length > 0)
            {
                entryFunc = shaderDetails.DebugInfo.entryFunc;

                foreach (var s in shaderDetails.DebugInfo.files)
                    files.Add(s.BaseFilename, s.filetext);

                int entryFile = shaderDetails.DebugInfo.entryFile;
                if (entryFile < 0 || entryFile >= shaderDetails.DebugInfo.files.Length)
                    entryFile = 0;

                mainfile = shaderDetails.DebugInfo.files[entryFile].BaseFilename;
            }
            else
            {
                var nl = Environment.NewLine;
                var nl2 = Environment.NewLine + Environment.NewLine;

                string hlsl = "// No HLSL available - function stub generated" + nl2;

                var shType = String.Format("{0}S", stage.stage.ToString()[0]);

                foreach (var res in shaderDetails.Resources)
                {
                    if (res.IsSampler)
                    {
                        hlsl += String.Format("//SamplerComparisonState {0} : register(s{1}); // can't disambiguate", res.name, res.bindPoint) + nl;
                        hlsl += String.Format("SamplerState {0} : register(s{1}); // can't disambiguate", res.name, res.bindPoint) + nl;
                    }
                    else
                    {
                        char regChar = 't';

                        if (res.IsReadWrite)
                        {
                            hlsl += "RW";
                            regChar = 'u';
                        }

                        if (res.IsTexture)
                        {
                            hlsl += String.Format("{0}<{1}> {2} : register({3}{4});", res.resType.ToString(), res.variableType.descriptor.name, res.name, regChar, res.bindPoint) + nl;
                        }
                        else
                        {
                            if (res.variableType.descriptor.rows == 1)
                                hlsl += String.Format("Buffer<{0}> {1} : register({2}{3});", res.variableType.descriptor.name, res.name, regChar, res.bindPoint) + nl;
                            else
                                hlsl += String.Format("StructuredBuffer<{0}> {1} : register({2}{3});", res.variableType.descriptor.name, res.name, regChar, res.bindPoint) + nl;
                        }
                    }
                }

                hlsl += nl2;

                string cbuffers = "";

                int cbufIdx = 0;
                foreach (var cbuf in shaderDetails.ConstantBlocks)
                {
                    if (cbuf.name.Length > 0 && cbuf.variables.Length > 0)
                    {
                        cbuffers += String.Format("cbuffer {0} : register(b{1}) {{", cbuf.name, cbufIdx) + nl;
                        MakeShaderVariablesHLSL(true, cbuf.variables, ref cbuffers, ref hlsl);
                        cbuffers += "};" + nl2;
                    }
                    cbufIdx++;
                }

                hlsl += cbuffers + nl2;

                hlsl += String.Format("struct {0}Input{1}{{{1}", shType, nl);
                foreach(var sig in shaderDetails.InputSig)
                    hlsl += String.Format("\t{0} {1} : {2};" + nl, sig.TypeString, sig.varName.Length > 0 ? sig.varName : ("param" + sig.regIndex), sig.D3D11SemanticString);
                hlsl += "};" + nl2;

                hlsl += String.Format("struct {0}Output{1}{{{1}", shType, nl);
                foreach (var sig in shaderDetails.OutputSig)
                    hlsl += String.Format("\t{0} {1} : {2};" + nl, sig.TypeString, sig.varName.Length > 0 ? sig.varName : ("param" + sig.regIndex), sig.D3D11SemanticString);
                hlsl += "};" + nl2;

                hlsl += String.Format("{0}Output {1}(in {0}Input IN){2}{{{2}\t{0}Output OUT = ({0}Output)0;{2}{2}\t// ...{2}{2}\treturn OUT;{2}}}{2}", shType, entryFunc, nl);

                mainfile = "generated.hlsl";

                files.Add(mainfile, hlsl);
            }

            if (files.Count == 0)
                return;

            ShaderViewer sv = new ShaderViewer(m_Core, false, entryFunc, files,

            // Save Callback
            (ShaderViewer viewer, Dictionary<string, string> updatedfiles) =>
            {
                string compileSource = updatedfiles[mainfile];

                // try and match up #includes against the files that we have. This isn't always
                // possible as fxc only seems to include the source for files if something in
                // that file was included in the compiled output. So you might end up with
                // dangling #includes - we just have to ignore them
                int offs = compileSource.IndexOf("#include");

                while(offs >= 0)
                {
                    // search back to ensure this is a valid #include (ie. not in a comment).
                    // Must only see whitespace before, then a newline.
                    int ws = Math.Max(0, offs-1);
                    while (ws >= 0 && (compileSource[ws] == ' ' || compileSource[ws] == '\t'))
                        ws--;

                    // not valid? jump to next.
                    if (ws > 0 && compileSource[ws] != '\n')
                    {
                        offs = compileSource.IndexOf("#include", offs + 1);
                        continue;
                    }

                    int start = ws+1;
                    
                    bool tail = true;

                    int lineEnd = compileSource.IndexOf("\n", start+1);
                    if(lineEnd == -1)
                    {
                        lineEnd = compileSource.Length;
                        tail = false;
                    }

                    ws = offs + "#include".Length;
                    while (compileSource[ws] == ' ' || compileSource[ws] == '\t')
                        ws++;

                    string line = compileSource.Substring(offs, lineEnd-offs+1);

                    if (compileSource[ws] != '<' && compileSource[ws] != '"')
                    {
                        viewer.ShowErrors("Invalid #include directive found:\r\n" + line);
                        return;
                    }

                    // find matching char, either <> or "";
                    int end = compileSource.IndexOf(compileSource[ws] == '"' ? '"' : '>', ws + 1);

                    if (end == -1)
                    {
                        viewer.ShowErrors("Invalid #include directive found:\r\n" + line);
                        return;
                    }

                    string fname = compileSource.Substring(ws + 1, end - ws - 1);

                    string fileText = "";

                    if (updatedfiles.ContainsKey(fname))
                        fileText = updatedfiles[fname];
                    else
                        fileText = "// Can't find file " + fname + "\n";

                    compileSource = compileSource.Substring(0, offs) + "\n\n" + fileText + "\n\n" + (tail ? compileSource.Substring(lineEnd + 1) : "");

                    // need to start searching from the beginning - wasteful but allows nested includes to work
                    offs = compileSource.IndexOf("#include");
                }

                if (updatedfiles.ContainsKey("@cmdline"))
                    compileSource = updatedfiles["@cmdline"] + "\n\n" + compileSource;

                // invoke off to the ReplayRenderer to replace the log's shader
                // with our edited one
                m_Core.Renderer.BeginInvoke((ReplayRenderer r) =>
                {
                    string errs = "";

                    uint flags = shaderDetails.DebugInfo.compileFlags;

                    ResourceId from = stage.Shader;
                    ResourceId to = r.BuildTargetShader(entryFunc, compileSource, flags, stage.stage, out errs);

                    viewer.BeginInvoke((MethodInvoker)delegate { viewer.ShowErrors(errs); });
                    if (to == ResourceId.Null)
                    {
                        r.RemoveReplacement(from);
                    }
                    else
                    {
                        r.ReplaceResource(from, to);
                    }
                });
            },

            // Close Callback
            () =>
            {
                // remove the replacement on close (we could make this more sophisticated if there
                // was a place to control replaced resources/shaders).
                m_Core.Renderer.BeginInvoke((ReplayRenderer r) =>
                {
                    r.RemoveReplacement(stage.Shader);
                });
            });

            sv.Show(m_DockContent.DockPanel);
        }

        private void ShowCBuffer(VulkanPipelineState.ShaderStage stage, UInt32 slot)
        {
        }

        private void cbuffers_NodeDoubleClicked(TreelistView.Node node)
        {
            VulkanPipelineState.ShaderStage stage = GetStageForSender(node.OwnerView);

            if (stage != null && node.Tag is UInt32)
            {
                ShowCBuffer(stage, (UInt32)node.Tag);
            }
        }

        private void CBuffers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            VulkanPipelineState.ShaderStage stage = GetStageForSender(sender);

            object tag = ((DataGridView)sender).Rows[e.RowIndex].Tag;

            if (stage != null && tag is UInt32)
            {
                ShowCBuffer(stage, (UInt32)tag);
            }
        }

        private string FormatMembers(int indent, string nameprefix, ShaderConstant[] vars)
        {
            string indentstr = new string(' ', indent*4);

            string ret = "";

            int i = 0;

            foreach (var v in vars)
            {
                if (v.type.members.Length > 0)
                {
                    if (i > 0)
                        ret += "\n";
                    ret += indentstr + "// struct " + v.type.Name + Environment.NewLine;
                    ret += indentstr + "{" + Environment.NewLine +
                                FormatMembers(indent + 1, v.name + "_", v.type.members) +
                           indentstr + "}" + Environment.NewLine;
                    if (i < vars.Length-1)
                        ret += Environment.NewLine;
                }
                else
                {
                    string arr = "";
                    if (v.type.descriptor.elements > 1)
                        arr = String.Format("[{0}]", v.type.descriptor.elements);
                    ret += indentstr + v.type.Name + " " + nameprefix + v.name + arr + ";" + Environment.NewLine;
                }

                i++;
            }

            return ret;
        }

        private void shaderCog_MouseEnter(object sender, EventArgs e)
        {
            if (sender is PictureBox)
            {
                VulkanPipelineState.ShaderStage stage = GetStageForSender(sender);

                if (stage != null && stage.Shader != ResourceId.Null)
                    (sender as PictureBox).Image = global::renderdocui.Properties.Resources.action_hover;
            }

            if (sender is Label)
            {
                VulkanPipelineState.ShaderStage stage = GetStageForSender(sender);

                if (stage == null) return;

                if (stage.stage == ShaderStageType.Vertex) shaderCog_MouseEnter(vsShaderCog, e);
                if (stage.stage == ShaderStageType.Tess_Control) shaderCog_MouseEnter(dsShaderCog, e);
                if (stage.stage == ShaderStageType.Tess_Eval) shaderCog_MouseEnter(hsShaderCog, e);
                if (stage.stage == ShaderStageType.Geometry) shaderCog_MouseEnter(gsShaderCog, e);
                if (stage.stage == ShaderStageType.Fragment) shaderCog_MouseEnter(psShaderCog, e);
                if (stage.stage == ShaderStageType.Compute) shaderCog_MouseEnter(csShaderCog, e);
            }
        }

        private void shaderCog_MouseLeave(object sender, EventArgs e)
        {
            if (sender is PictureBox)
            {
                (sender as PictureBox).Image = global::renderdocui.Properties.Resources.action;
            }

            if (sender is Label)
            {
                VulkanPipelineState.ShaderStage stage = GetStageForSender(sender);

                if (stage == null) return;

                if (stage.stage == ShaderStageType.Vertex) shaderCog_MouseEnter(vsShaderCog, e);
                if (stage.stage == ShaderStageType.Tess_Control) shaderCog_MouseEnter(dsShaderCog, e);
                if (stage.stage == ShaderStageType.Tess_Eval) shaderCog_MouseEnter(hsShaderCog, e);
                if (stage.stage == ShaderStageType.Geometry) shaderCog_MouseEnter(gsShaderCog, e);
                if (stage.stage == ShaderStageType.Fragment) shaderCog_MouseEnter(psShaderCog, e);
                if (stage.stage == ShaderStageType.Compute) shaderCog_MouseEnter(csShaderCog, e);
            }
        }

        private void pipeFlow_SelectedStageChanged(object sender, EventArgs e)
        {
            stageTabControl.SelectedIndex = pipeFlow.SelectedStage;
        }

        private void csDebug_Click(object sender, EventArgs e)
        {
            uint gx = 0, gy = 0, gz = 0;
            uint tx = 0, ty = 0, tz = 0;

            if (m_Core.CurVulkanPipelineState == null ||
                m_Core.CurVulkanPipelineState.CS.Shader == ResourceId.Null ||
                m_Core.CurVulkanPipelineState.CS.ShaderDetails == null)
                return;

            if (uint.TryParse(groupX.Text, out gx) &&
                uint.TryParse(groupY.Text, out gy) &&
                uint.TryParse(groupZ.Text, out gz) &&
                uint.TryParse(threadX.Text, out tx) &&
                uint.TryParse(threadY.Text, out ty) &&
                uint.TryParse(threadZ.Text, out tz))
            {
                uint[] groupdim = m_Core.CurDrawcall.dispatchDimension;
                uint[] threadsdim = m_Core.CurDrawcall.dispatchThreadsDimension;

                for (int i = 0; i < 3; i++)
                    if (threadsdim[i] == 0)
                        threadsdim[i] = m_Core.CurVulkanPipelineState.CS.ShaderDetails.DispatchThreadsDimension[i];

                string debugContext = String.Format("Group [{0},{1},{2}] Thread [{3},{4},{5}]", gx, gy, gz, tx, ty, tz);

                if (gx >= groupdim[0] ||
                    gy >= groupdim[1] ||
                    gz >= groupdim[2] ||
                    tx >= threadsdim[0] ||
                    ty >= threadsdim[1] ||
                    tz >= threadsdim[2])
                {
                    string bounds = String.Format("Group Dimensions [{0},{1},{2}] Thread Dimensions [{3},{4},{5}]",
                        groupdim[0], groupdim[1], groupdim[2],
                        threadsdim[0], threadsdim[1], threadsdim[2]);

                    MessageBox.Show(String.Format("{0} is out of bounds\n{1}", debugContext, bounds), "Couldn't debug compute shader.",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ShaderDebugTrace trace = null;

                ShaderReflection shaderDetails = m_Core.CurVulkanPipelineState.CS.ShaderDetails;

                m_Core.Renderer.Invoke((ReplayRenderer r) =>
                {
                    trace = r.DebugThread(new uint[] { gx, gy, gz }, new uint[] { tx, ty, tz });
                });

                if (trace == null || trace.states.Length == 0)
                {
                    MessageBox.Show("Couldn't debug compute shader.", "Uh Oh!",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                this.BeginInvoke(new Action(() =>
                {
                    ShaderViewer s = new ShaderViewer(m_Core, shaderDetails, ShaderStageType.Compute, trace, debugContext);

                    s.Show(m_DockContent.DockPanel);
                }));
            }
            else
            {
                MessageBox.Show("Enter numbers for group and thread ID.", "Invalid thread", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void meshView_MouseEnter(object sender, EventArgs e)
        {
            meshView.BackColor = Color.LightGray;
        }

        private void meshView_MouseLeave(object sender, EventArgs e)
        {
            meshView.BackColor = SystemColors.Control;
        }

        private void meshView_Click(object sender, EventArgs e)
        {
            (new BufferViewer(m_Core, true)).Show(m_DockContent.DockPanel);
        }

        private float GetHueForVB(int i)
        {
            int idx = ((i+1) * 21) % 32; // space neighbouring colours reasonably distinctly
            return (float)(idx) / 32.0f;
        }

        private void HighlightIABind(uint slot)
        {
            var VI = m_Core.CurVulkanPipelineState.VI;
        
            Color c = HSLColor(GetHueForVB((int)slot), 1.0f, 0.95f);
            
            if (slot < m_VBNodes.Count)
                m_VBNodes[(int)slot].DefaultBackColor = c;

            if (slot < m_BindNodes.Count)
                m_BindNodes[(int)slot].DefaultBackColor = c;

            for (int i = 0; i < viAttrs.Nodes.Count; i++)
            {
                var n = viAttrs.Nodes[i];
                if (VI.attrs[i].binding == slot)
                    n.DefaultBackColor = c;
                else
                    n.DefaultBackColor = Color.Transparent;
            }

            viAttrs.Invalidate();
            viBindings.Invalidate();
            viBuffers.Invalidate();
        }

        private void iaattrs_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_Core.CurVulkanPipelineState == null) return;

            Point mousePoint = viAttrs.PointToClient(Cursor.Position);
            var hoverNode = viAttrs.CalcHitNode(mousePoint);

            ia_MouseLeave(sender, e);

            var VI = m_Core.CurVulkanPipelineState.VI;

            if (hoverNode != null)
            {
                int index = viAttrs.Nodes.GetNodeIndex(hoverNode);

                if (index >= 0 && index < VI.attrs.Length)
                {
                    uint binding = VI.attrs[index].binding;

                    HighlightIABind(binding);
                }
            }
        }

        private void iabinds_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_Core.CurVulkanPipelineState == null) return;

            Point mousePoint = viBindings.PointToClient(Cursor.Position);
            var hoverNode = viBindings.CalcHitNode(mousePoint);

            ia_MouseLeave(sender, e);

            if (hoverNode != null)
            {
                int idx = m_BindNodes.IndexOf(hoverNode);
                if (idx >= 0)
                    HighlightIABind((uint)idx);
                else
                    hoverNode.DefaultBackColor = SystemColors.ControlLight;
            }
        }

        private void iabuffers_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_Core.CurVulkanPipelineState == null) return;

            Point mousePoint = viBuffers.PointToClient(Cursor.Position);
            var hoverNode = viBuffers.CalcHitNode(mousePoint);

            ia_MouseLeave(sender, e);

            if (hoverNode != null)
            {
                int idx = m_VBNodes.IndexOf(hoverNode);
                if (idx >= 0)
                    HighlightIABind((uint)idx);
                else
                    hoverNode.DefaultBackColor = SystemColors.ControlLight;
            }
        }

        private void ia_MouseLeave(object sender, EventArgs e)
        {
            foreach (var n in viBuffers.Nodes)
                n.DefaultBackColor = Color.Transparent;

            foreach (var n in viAttrs.Nodes)
                n.DefaultBackColor = Color.Transparent;

            foreach (var n in viBindings.Nodes)
                n.DefaultBackColor = Color.Transparent;

            viAttrs.Invalidate();
            viBindings.Invalidate();
            viBuffers.Invalidate();
        }

        private void export_Click(object sender, EventArgs e)
        {
            if (!m_Core.LogLoaded) return;
            
            DialogResult res = exportDialog.ShowDialog();

            if (res == DialogResult.OK)
            {
            }
        }
   }
}