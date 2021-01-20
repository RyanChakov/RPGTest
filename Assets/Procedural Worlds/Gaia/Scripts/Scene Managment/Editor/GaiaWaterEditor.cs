﻿using System.Collections.Generic;
using System.IO;
using Gaia.Internal;
using ProceduralWorlds.WaterSystem;
using PWCommon4;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using UnityEngine.Rendering;

namespace Gaia
{
    [CustomEditor(typeof(GaiaSceneWater))]
    public class GaiaWaterEditor : PWEditor
    {
        #region Variables

        private EditorUtils m_editorUtils;
        private GaiaSettings m_gaiaSettings;
        private SceneProfile m_profile;
        private GaiaWaterProfileValues m_profileValues;
        private PWS_WaterSystem m_waterSystemPro;
        private GaiaConstants.EnvironmentRenderer m_renderPipeline;
        private List<string> m_profileList = new List<string>();
        private GUIStyle m_boxStyle;
        [SerializeField]
        private Material m_gaiaOceanMat;
        private int newProfileListIndex = 1;
        private bool updateSettingsOnly;
        private Texture2D m_waterTexture;
        private bool enableEditMode;
        public Gradient gradient;
        private GaiaSceneWater m_sceneWater;

        #endregion

        #region Unity Functions

        private void OnEnable()
        {
            //Get Gaia Lighting Profile object
            if (GaiaGlobal.Instance != null)
            {
                m_profile = GaiaGlobal.Instance.SceneProfile;
            }

#if !GAIA_PRO_PRESENT
            if (m_profile != null)
            {
                for (int i = 0; i < m_profile.m_customRenderDistances.Length; i++)
                {
                    m_profile.m_customRenderDistances[i] = 0f;
                }

                m_profile.m_customRenderDistance = 0f;
            }
#endif
            if (m_editorUtils == null)
            {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            m_renderPipeline = m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled;

            m_waterSystemPro = Object.FindObjectOfType<PWS_WaterSystem>();

            newProfileListIndex = m_gaiaSettings.m_gaiaWaterProfile.m_selectedWaterProfileValuesIndex;
            if (newProfileListIndex < 0)
            {
                newProfileListIndex = 1;
            }

            m_gaiaOceanMat = GaiaWater.GetGaiaOceanMaterial();

            enableEditMode = Directory.Exists(GaiaUtils.GetAssetPath("Dev Utilities"));
        }
        /// <summary>
        /// Setup on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }
        }
        public override void OnInspectorGUI()
        {
            //Initialization
            m_editorUtils.Initialize(); // Do not remove this!

            if (m_sceneWater == null)
            {
                m_sceneWater = (GaiaSceneWater) target;
            }

            //Set up the box style
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = {textColor = GUI.skin.label.normal.textColor},
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
            }

            if (GaiaGlobal.Instance != null)
            {
                m_profile = GaiaGlobal.Instance.SceneProfile;
            }

            if (m_gaiaSettings == null)
            {
                m_gaiaSettings = GaiaUtils.GetGaiaSettings();
            }

            if (m_renderPipeline != m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled)
            {
                m_renderPipeline = m_gaiaSettings.m_pipelineProfile.m_activePipelineInstalled;
            }

            if (m_profile.m_waterProfiles.Count > 0)
            {
                m_editorUtils.Panel("ProfileSettings", ProfileSettings, false, true, true);
            }
            else
            {
                EditorGUILayout.HelpBox("No Water profile have been set in the Scene Profile. Please go to Gaia Runtime and go to 'Save And Load' and click Revert To Defaults. Or go to the Gaia Manager and go to Runtime in Standard tab and click 'Create/Update Runtime'", MessageType.Info);
            }
        }

        #endregion

        #region Utils

        private void ProfileSettings(bool helpEnabled)
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("EditWaterInPlayMode"), MessageType.Warning);
            }
            if (enableEditMode)
            {
                m_profile.m_waterEditSettings = EditorGUILayout.ToggleLeft(new GUIContent("Use Procedural Worlds Editor Settings", "Enable PW Editor features used for development. Users will not see or be able to use this featue."), m_profile.m_waterEditSettings);
            }
            else
            {
                m_profile.m_waterEditSettings = false;
            }

            GaiaConstants.GlobalSystemMode mode = m_profile.m_waterSystemMode;
            mode = (GaiaConstants.GlobalSystemMode)m_editorUtils.EnumPopup("WaterSystemMode", mode, helpEnabled);
            if (mode != m_profile.m_waterSystemMode)
            {
                m_profile.m_waterSystemMode = mode;
                switch (m_profile.m_waterSystemMode)
                {
                    case GaiaConstants.GlobalSystemMode.Gaia:
                    {
                        if (m_profile.m_selectedProfile != "None")
                        {
                            GaiaWater.GetProfile(m_profile.m_selectedWaterProfileValuesIndex, m_gaiaOceanMat, m_profile, true, false);
                            updateSettingsOnly = false;
                        }

                        break;
                    }
                    case GaiaConstants.GlobalSystemMode.ThirdParty:
                        GaiaUtils.RemoveWaterSystems();
                        break;
                    case GaiaConstants.GlobalSystemMode.None:
                        GaiaUtils.RemoveWaterSystems();
                        GaiaUtils.RemoveAquas(GaiaUtils.GetCamera(), GaiaConstants.GlobalSystemMode.None);
                        break;
                }
            }

            EditorGUILayout.BeginVertical(m_boxStyle);
            if (m_profile.m_waterSystemMode == GaiaConstants.GlobalSystemMode.Gaia)
            {
                //Monitor for changes
                EditorGUI.BeginChangeCheck();

                //Setup Profiles
                m_profileList.Clear();
                if (m_profile.m_waterProfiles.Count > 0)
                {
                    foreach (GaiaWaterProfileValues profile in m_profile.m_waterProfiles)
                    {
                        m_profileList.Add(profile.m_typeOfWater);
                    }
                }
                m_profileList.Add("None");

                newProfileListIndex = m_profile.m_selectedWaterProfileValuesIndex;
                //Get Profile values
                if (newProfileListIndex > m_profileList.Count)
                {
                    newProfileListIndex = 0;
                }

                if (m_profileList[newProfileListIndex] == "None")
                {
                    m_profile.m_selectedWaterProfileValuesIndex = EditorGUILayout.Popup("Water Profile", m_profile.m_selectedWaterProfileValuesIndex, m_profileList.ToArray());
                    EditorGUILayout.HelpBox("No water profile selected", MessageType.Info);
                }
                else
                {
                    m_profileValues = m_profile.m_waterProfiles[newProfileListIndex];

                    EditorGUILayout.BeginHorizontal();
                    if (m_profile.m_renamingProfile)
                    {
                        m_profileValues.m_profileRename = m_editorUtils.TextField("NewProfileName", m_profileValues.m_profileRename);
                    }
                    else
                    {
                        m_profile.m_selectedWaterProfileValuesIndex = EditorGUILayout.Popup("Water Profile", m_profile.m_selectedWaterProfileValuesIndex, m_profileList.ToArray());
                    }
                    if (m_profileValues.m_userCustomProfile)
                    {
                        if (m_profile.m_renamingProfile)
                        {
                            if (m_editorUtils.Button("Save", GUILayout.MaxWidth(55f)))
                            {
                                m_profileValues.m_typeOfWater = m_profileValues.m_profileRename;
                                m_profile.m_selectedProfile = m_profileValues.m_profileRename;
                                m_profile.m_renamingProfile = false;
                            }
                        }
                        else
                        {
                            if (m_editorUtils.Button("Rename", GUILayout.MaxWidth(55f)))
                            {
                                m_profile.m_renamingProfile = true;
                                m_profileValues.m_profileRename = m_profileValues.m_typeOfWater;
                            }
                        }

                        if (m_editorUtils.Button("Remove", GUILayout.MaxWidth(30f)))
                        {
                            m_profile.m_renamingProfile = false;
                            m_profile.m_waterProfiles.RemoveAt(m_profile.m_selectedWaterProfileValuesIndex);
                            m_profile.m_selectedWaterProfileValuesIndex--;
                        }
                    }
                    if (m_editorUtils.Button("MakeCopy", GUILayout.MaxWidth(30f)))
                    {
                        AddNewCustomProfile();
                    }
                    EditorGUILayout.EndHorizontal();
                    m_editorUtils.InlineHelp("WaterProfile", helpEnabled);

                    if (m_profileList[newProfileListIndex] == "None")
                    {
                        EditorGUILayout.HelpBox("No water profile selected", MessageType.Info);
                    }
                    else
                    {
                        m_editorUtils.Panel("WaterProfileSettings", WaterProfileSettingsEnabled, true);
                        m_editorUtils.Panel("GlobalSettings", GlobalSettingsEnabled);
                        m_editorUtils.Panel("ReflectionSettings", ReflectionSettingsEnabled);
                        m_editorUtils.Panel("UnderwaterSettings", UnderwaterSettingsEnabled);
                        m_editorUtils.Panel("MeshQualitySettings", WaterMeshQualityEnabled);
                    }

                    //Check for changes, make undo record, make changes and let editor know we are dirty
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(m_profile, "Made changes");
                        EditorUtility.SetDirty(m_profile);

                        if (m_profile.m_waterUpdateInRealtime)
                        {
                            if (m_profile.m_selectedProfile != "None")
                            {
                                GaiaWater.GetProfile(m_profile.m_selectedWaterProfileValuesIndex, m_gaiaOceanMat, m_profile, true, updateSettingsOnly);
                                updateSettingsOnly = false;
                            }
                        }
                    }

                    m_editorUtils.Panel("SaveAndLoad", SaveAndLoad);
                }
            }
            else if (m_profile.m_waterSystemMode == GaiaConstants.GlobalSystemMode.ThirdParty)
            {
                m_editorUtils.Panel("ThirdPartySettings", ThirdPartyPanel, true);
            }
            else
            {
                EditorGUILayout.HelpBox("System Mode is set to 'None' to use water set the mode to 'Gaia or ThirdParty'", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }
        private void GlobalSettingsEnabled(bool helpEnabled)
        {
            m_profile.m_enableGPUInstancing = m_editorUtils.Toggle("EnableGPUInstancing", m_profile.m_enableGPUInstancing, helpEnabled);
            m_profile.InfiniteMode = m_editorUtils.Toggle("InfiniteMode", m_profile.InfiniteMode, helpEnabled);
            m_profile.m_waterPrefab = (GameObject)m_editorUtils.ObjectField("WaterPrefab", m_profile.m_waterPrefab, typeof(GameObject), false, helpEnabled, GUILayout.Height(16f));
            if (m_profile.m_waterPrefab == null)
            {
                EditorGUILayout.HelpBox("Missing a Water Prefab, please add one.", MessageType.Error);
            }
            m_profile.m_transitionFXPrefab = (GameObject)m_editorUtils.ObjectField("TransitionFXPrefab", m_profile.m_transitionFXPrefab, typeof(GameObject), false, helpEnabled, GUILayout.Height(16f));
            if (m_profile.m_transitionFXPrefab == null)
            {
                EditorGUILayout.HelpBox("Missing a Transition FX Prefab, please add one.", MessageType.Error);
            }
            if (!m_profile.m_enableGPUInstancing)
            {
                EditorGUILayout.HelpBox("Enabling GPU instancing will help improve performance. We recommend you enable this setting.", MessageType.Info);
            }
        }
        private void ReflectionSettingsEnabled(bool helpEnabled)
        {
            m_profile.m_enableReflections = m_editorUtils.Toggle("EnableReflections", m_profile.m_enableReflections);
            if (m_profile.m_enableReflections)
            {
#if GAIA_XR
                if (m_profile.m_controllerType == GaiaConstants.EnvironmentControllerType.XRController)
                {
                    EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("XRWaterReflections"), MessageType.Warning);
                }
#endif
                if (m_renderPipeline != GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                     if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
                     {
                         m_editorUtils.Heading("ReflectionRendering");
                         m_profile.m_autoRefresh = m_editorUtils.Toggle("AutoRefresh", m_profile.m_autoRefresh, helpEnabled);
                         if (m_profile.m_autoRefresh)
                         {
                             EditorGUI.indentLevel++;
                             m_profile.m_autoUpdateMode = (GaiaConstants.WaterAutoUpdateMode)m_editorUtils.EnumPopup("AutoUpdateMode", m_profile.m_autoUpdateMode, helpEnabled);
                             if (m_profile.m_autoUpdateMode == GaiaConstants.WaterAutoUpdateMode.Interval)
                             {
                                 m_profile.m_ignoreSceneConditions = m_editorUtils.Toggle("IgnoreSceneConditions", m_profile.m_ignoreSceneConditions, helpEnabled);
                                 m_profile.m_refreshRate = m_editorUtils.FloatField("RefreshRate", m_profile.m_refreshRate, helpEnabled);
                                 if (m_profile.m_refreshRate < 0.01f)
                                 {
                                     m_profile.m_refreshRate = 0.01f;
                                 }
                             }
                             EditorGUI.indentLevel--;
                         }
                         m_profile.m_reflectionSettingsData.m_Shadows = m_editorUtils.Toggle("EnableReflectionShadows", m_profile.m_reflectionSettingsData.m_Shadows, helpEnabled);
                         m_profile.m_reflectionResolution = (GaiaConstants.GaiaProWaterReflectionsQuality)m_editorUtils.EnumPopup("ReflectionResolution", m_profile.m_reflectionResolution, helpEnabled);
                         m_profile.UpdateTextureResolution();
                         m_profile.m_reflectionSettingsData.m_ResolutionMultiplier = (GaiaConstants.ResolutionMulltiplier)m_editorUtils.EnumPopup("ReflectionResolutionMultiplier", m_profile.m_reflectionSettingsData.m_ResolutionMultiplier, helpEnabled);
                         m_profile.m_clipPlaneOffset = m_editorUtils.Slider("ClipPlaneOffset", m_profile.m_clipPlaneOffset, -5f, 100f, helpEnabled);
                         m_profile.m_reflectedLayers = LayerMaskField("ReflectedLayers", m_editorUtils, m_profile.m_reflectedLayers);
                         m_profile.m_reflectionSettingsData.m_ReflectLayers = m_profile.m_reflectedLayers;
                         m_profile.m_reflectionSettingsData.m_textureResolution = m_profile.m_textureResolution;

                         m_profile.m_reflectionSettingsData.m_enableRenderDistance = m_editorUtils.Toggle("UseCustomRenderDistance", m_profile.m_reflectionSettingsData.m_enableRenderDistance, helpEnabled);
                         if ( m_profile.m_reflectionSettingsData.m_enableRenderDistance)
                         {
                             EditorGUI.indentLevel++;
                             m_profile.m_reflectionSettingsData.m_enableRenderDistances = m_editorUtils.Toggle("EnableMultiLayerDistance", m_profile.m_reflectionSettingsData.m_enableRenderDistances, helpEnabled);
                             if (m_profile.m_reflectionSettingsData.m_enableRenderDistances)
                             {
                                 GetUnityLayers(out var layers, out var layerCount);
                                 for (int i = 0; i < layerCount; i++)
                                 {
                                     EditorGUI.indentLevel++;
                                     EditorGUILayout.BeginHorizontal();
                                     EditorGUILayout.LabelField(layers[i]);
                                     m_profile.m_reflectionSettingsData.m_customRenderDistances[i] = EditorGUILayout.FloatField(m_profile.m_reflectionSettingsData.m_customRenderDistances[i]);
                                     EditorGUILayout.EndHorizontal();
                                     EditorGUI.indentLevel--;
                                 }
                             }
                             else
                             {
                                 EditorGUI.indentLevel++;
                                 m_profile.m_reflectionSettingsData.m_customRenderDistance = m_editorUtils.FloatField("CustomRenderDistance", m_profile.m_reflectionSettingsData.m_customRenderDistance, helpEnabled);
                                 EditorGUI.indentLevel--;
                             }
                             EditorGUI.indentLevel--;
                         }
                     }
                     else
                     {
                         m_editorUtils.Heading("ReflectionRendering");
                         m_profile.m_reflectionResolution = (GaiaConstants.GaiaProWaterReflectionsQuality)m_editorUtils.EnumPopup("ReflectionResolution", m_profile.m_reflectionResolution, helpEnabled);
                         m_profile.UpdateTextureResolution();
                         m_profile.m_reflectedLayers = LayerMaskField("ReflectedLayers", m_editorUtils, m_profile.m_reflectedLayers);
                         m_profile.m_clipPlaneOffset = m_editorUtils.Slider("ClipPlaneOffset", m_profile.m_clipPlaneOffset, -5f, 100f, helpEnabled);
                         m_profile.m_hdrpReflectionIntensity = m_editorUtils.Slider("HDRPReflectionIntensity", m_profile.m_hdrpReflectionIntensity, 0f, 5f, helpEnabled);
                     }
                }
                else
                {
                    EditorGUILayout.HelpBox("Reflections render the object reflections on the water surface. This can be very expensive feature and could cause performance issue on low end machines.", MessageType.Info);

                    GUILayout.Space(15f);
                    m_editorUtils.Heading("ReflectionSupport");
                    m_profile.m_autoRefresh = m_editorUtils.Toggle("AutoRefresh", m_profile.m_autoRefresh, helpEnabled);
                    if (m_profile.m_autoRefresh)
                    {
                        EditorGUI.indentLevel++;
                        m_profile.m_autoUpdateMode = (GaiaConstants.WaterAutoUpdateMode)m_editorUtils.EnumPopup("AutoUpdateMode", m_profile.m_autoUpdateMode, helpEnabled);
                        if (m_profile.m_autoUpdateMode == GaiaConstants.WaterAutoUpdateMode.Interval)
                        {
                            m_profile.m_ignoreSceneConditions = m_editorUtils.Toggle("IgnoreSceneConditions", m_profile.m_ignoreSceneConditions, helpEnabled);
                            m_profile.m_refreshRate = m_editorUtils.FloatField("RefreshRate", m_profile.m_refreshRate, helpEnabled);
                            if (m_profile.m_refreshRate < 0.01f)
                            {
                                m_profile.m_refreshRate = 0.01f;
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                    m_profile.m_allowMSAA = m_editorUtils.Toggle("AllowMSAA", m_profile.m_allowMSAA, helpEnabled);
                    m_profile.m_useHDR = m_editorUtils.Toggle("UseHDR", m_profile.m_useHDR, helpEnabled);
                    m_profile.m_disablePixelLights = m_editorUtils.Toggle("DisablePixelLights", m_profile.m_disablePixelLights, helpEnabled);
                    m_profile.m_enableDisabeHeightFeature = m_editorUtils.Toggle("EnableHeightFeatures", m_profile.m_enableDisabeHeightFeature, helpEnabled);
                    if (m_profile.m_enableDisabeHeightFeature)
                    {
                        EditorGUI.indentLevel++;
                        m_profile.m_disableHeight = m_editorUtils.FloatField("DisableHeight", m_profile.m_disableHeight, helpEnabled);
                        EditorGUI.indentLevel--;
                    }
                    m_profile.m_useCustomRenderDistance = m_editorUtils.Toggle("UseCustomRenderDistance", m_profile.m_useCustomRenderDistance, helpEnabled);
                    if (m_profile.m_useCustomRenderDistance)
                    {
                        EditorGUI.indentLevel++;
                        m_profile.m_enableLayerDistances = m_editorUtils.Toggle("EnableMultiLayerDistance", m_profile.m_enableLayerDistances, helpEnabled);
                        if (m_profile.m_enableLayerDistances)
                        {
                            GetUnityLayers(out var layers, out var layerCount);
                            for (int i = 0; i < layerCount; i++)
                            {
                                EditorGUI.indentLevel++;
                                if (layers[i].Length > 0)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField(layers[i]);
                                    m_profile.m_customRenderDistances[i] = EditorGUILayout.FloatField(m_profile.m_customRenderDistances[i]);
                                    EditorGUILayout.EndHorizontal();
                                }
                                EditorGUI.indentLevel--;
                            }
                        }
                        else
                        {
                            EditorGUI.indentLevel++;
                            m_profile.m_customRenderDistance = m_editorUtils.FloatField("CustomRenderDistance", m_profile.m_customRenderDistance, helpEnabled);
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }

                    GUILayout.Space(15f);
                    m_editorUtils.Heading("ReflectionRendering");
                    m_profile.m_reflectionResolution = (GaiaConstants.GaiaProWaterReflectionsQuality)m_editorUtils.EnumPopup("ReflectionResolution", m_profile.m_reflectionResolution, helpEnabled);
                    m_profile.UpdateTextureResolution();
                    m_profile.m_clipPlaneOffset = m_editorUtils.Slider("ClipPlaneOffset", m_profile.m_clipPlaneOffset, -5f, 100f, helpEnabled);
                    m_profile.m_reflectedLayers = LayerMaskField("ReflectedLayers", m_editorUtils, m_profile.m_reflectedLayers);
                    m_editorUtils.InlineHelp("ReflectedLayers", helpEnabled);
                }

                if (!m_profile.m_waterUpdateInRealtime)
                {
                    if (m_editorUtils.Button("UpdateReflectionSettings"))
                    {
                        GaiaWater.SetupWaterReflectionSettings(m_profile, m_profileValues, true);
                    }
                }
            }
            else
            {
                EditorGUI.indentLevel++;
                m_profile.m_disaleSkyboxReflection = m_editorUtils.Toggle("DisableSkyboxReflections", m_profile.m_disaleSkyboxReflection);
                m_profile.m_reflectionSettingsData.m_disableSkyboxReflections = m_profile.m_disaleSkyboxReflection;
                EditorGUI.indentLevel--;
            }

            m_profile.m_reflectionSettingsData.m_enableReflections = m_profile.m_enableReflections;
        }
        private void WaterMeshQualityEnabled(bool helpEnabled)
        {
            //m_profile.m_enableWaterMeshQuality = EditorGUILayout.ToggleLeft("Enable Water Mesh Quality", m_profile.m_enableWaterMeshQuality);
            m_profile.m_enableWaterMeshQuality = true;
            if (m_profile.m_enableWaterMeshQuality)
            {
                m_profile.m_waterMeshQuality = (GaiaConstants.WaterMeshQuality)m_editorUtils.EnumPopup("WaterMeshQuality", m_profile.m_waterMeshQuality, helpEnabled);
                m_profile.m_meshType = (GaiaConstants.MeshType)m_editorUtils.EnumPopup("MeshType", m_profile.m_meshType, helpEnabled);
                if (m_profile.m_waterMeshQuality == GaiaConstants.WaterMeshQuality.Custom)
                {
                    if (m_profile.m_meshType == GaiaConstants.MeshType.Plane)
                    {
                        m_profile.m_customMeshQuality = m_editorUtils.IntSlider("CustomMeshQuality", m_profile.m_customMeshQuality, 1, 16, helpEnabled);
                    }
                    else
                    {
                        m_profile.m_customMeshQuality = m_editorUtils.IntSlider("CustomMeshQuality", m_profile.m_customMeshQuality, 1, 16, helpEnabled);
                        //m_profile.m_customMeshQuality = m_editorUtils.IntSlider("CustomMeshQuality", (int)m_profile.m_customMeshQuality * 2, 2, 256) / 2;
                    }

                }

                if (m_profile.m_meshType == GaiaConstants.MeshType.Plane)
                {
                    m_profile.m_xSize = m_editorUtils.IntField("WaterSize", m_profile.m_xSize, helpEnabled);
                    m_profile.m_zSize = m_profile.m_xSize;
                }
                else
                {
                    m_profile.m_xSize = m_editorUtils.IntField("WaterSize", m_profile.m_xSize, helpEnabled);
                    m_profile.m_zSize = m_profile.m_xSize;
                }

                if (m_waterSystemPro == null)
                {
                    m_waterSystemPro = FindObjectOfType<PWS_WaterSystem>();
                }
                else
                {
                    m_waterSystemPro.m_MeshType = m_profile.m_meshType;
                    m_waterSystemPro.m_Size.x = m_profile.m_xSize;
                    m_waterSystemPro.m_Size.z = m_profile.m_zSize;
                    m_waterSystemPro.m_meshDensity.x = m_profile.m_customMeshQuality;
                    m_waterSystemPro.m_meshDensity.y = m_profile.m_customMeshQuality;
                    EditorGUILayout.LabelField(m_editorUtils.GetTextValue("PolyCountGenerated") + m_waterSystemPro.CalculatePolysRequired().ToString());
                }

                if (!m_profile.m_waterUpdateInRealtime)
                {
                    if (m_editorUtils.Button("UpdateWaterMeshQuality"))
                    {
                        GaiaWater.UpdateWaterMeshQuality(m_profile, m_profile.m_waterPrefab);
                    }
                }
            }
        }
        private void UnderwaterSettingsEnabled(bool helpEnabled)
        {
            if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                EditorGUILayout.HelpBox("Underwater effects are not yet supported in HDRP.", MessageType.Info);
            }
            else
            {
                m_editorUtils.Panel("CausticSettings", CausticSettingsEnabled);

                m_profile.m_underwaterParticles = (GameObject)m_editorUtils.ObjectField("UnderwaterParticlesPrefab", m_profile.m_underwaterParticles, typeof(GameObject), false, helpEnabled, GUILayout.Height(16f));
                if (m_profile.m_underwaterParticles == null)
                {
                    EditorGUILayout.HelpBox("Missing underwater particles prefab, please add one.", MessageType.Error);
                }
                m_profile.m_underwaterHorizonPrefab = (GameObject)m_editorUtils.ObjectField("UnderwaterHorizonPrefab", m_profile.m_underwaterHorizonPrefab, typeof(GameObject), false, helpEnabled, GUILayout.Height(16f));
                if (m_profile.m_underwaterHorizonPrefab == null)
                {
                    EditorGUILayout.HelpBox("Missing a underwater horizon prefab, please add one.", MessageType.Error);
                }
                m_profile.m_supportUnderwaterEffects = m_editorUtils.Toggle("SupportUnderwaterEffects", m_profile.m_supportUnderwaterEffects, helpEnabled);
                if (m_profile.m_supportUnderwaterEffects)
                {
                    m_profile.m_supportUnderwaterPostProcessing = m_editorUtils.Toggle("SupportUnderwaterPostProcessing", m_profile.m_supportUnderwaterPostProcessing, helpEnabled);

                    m_profile.m_supportUnderwaterFog = m_editorUtils.Toggle("SupportUnderwaterFog", m_profile.m_supportUnderwaterFog, helpEnabled);

                    m_profile.m_supportUnderwaterParticles = m_editorUtils.Toggle("SupportUnderwaterParticles", m_profile.m_supportUnderwaterParticles, helpEnabled);

                    if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Lightweight)
                    {
                        EditorGUILayout.HelpBox("Underwater effects are in 'Experimental' These will change and be updated over time. LWRP does work but you may experience some artifacts.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Underwater effects are in 'Experimental' These will change and be updated over time.", MessageType.Info);
                    }
                }
            }
        }
        private void WaterProfileSettingsEnabled(bool helpEnabled)
        {
            EditorGUI.BeginChangeCheck();

            if (!m_profileValues.m_userCustomProfile && !m_profile.m_waterEditSettings)
            {
                EditorGUILayout.HelpBox(m_editorUtils.GetTextValue("NewProfileInfo"), MessageType.Info);
                if (m_editorUtils.Button("CreateNewProfileButton"))
                {
                    AddNewCustomProfile();
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = false;
            }

            m_profile.m_waterUpdateInRealtime = m_editorUtils.ToggleLeft("UpdateInRealtime", m_profile.m_waterUpdateInRealtime);
            if (!m_profile.m_waterUpdateInRealtime)
            {
                if (m_editorUtils.Button("UpdateToScene"))
                {
                    if (m_profile.m_selectedProfile != "None")
                    {
                        GaiaWater.GetProfile(m_profile.m_selectedWaterProfileValuesIndex, m_gaiaOceanMat, m_profile, true, true);
                    }
                }
            }

            if (m_gaiaOceanMat != null)
            {
                //Profile setup
                if (!m_profileValues.m_userCustomProfile)
                {
                    m_profile.m_renamingProfile = false;
                }

                m_editorUtils.Heading("ColorSettings");
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                m_profileValues.m_colorDepthRamp = (Texture2D)m_editorUtils.ObjectField("BakedColorDepthRamp", m_profileValues.m_colorDepthRamp, typeof(Texture2D), false, false, GUILayout.Height(16f));
                if (m_editorUtils.Button("ClearBaked", GUILayout.Width(90f)))
                {
                    m_profileValues.m_colorDepthRamp = null;
                    PWS_WaterSystem pWS_WaterSystem = GameObject.FindObjectOfType<PWS_WaterSystem>();
                    if (pWS_WaterSystem != null)
                    {
                        pWS_WaterSystem.m_waterTexture = null;
                        pWS_WaterSystem.m_requestDepthTextureGeneration = true;
                    }
                }

                EditorGUILayout.EndHorizontal();
                m_editorUtils.InlineHelp("BakedColorDepthRamp", helpEnabled);
                if (m_profileValues.m_colorDepthRamp == null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    m_profileValues.m_waterGradient = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("WaterGradientColor"), m_editorUtils.GetTooltip("WaterGradientColor")), m_profileValues.m_waterGradient);
                    if (m_editorUtils.Button("RevertGradient", GUILayout.Width(90f)))
                    {
                        m_profileValues.m_waterGradient = CreateNewWaterSurfaceGradient(newProfileListIndex);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (m_waterSystemPro != null)
                        {
                            m_waterSystemPro.m_requestDepthTextureGeneration = true;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    m_editorUtils.InlineHelp("WaterGradientColor", helpEnabled);
                    EditorGUILayout.BeginHorizontal();
                    m_profileValues.m_gradientTextureResolution = Mathf.Clamp(EditorGUILayout.IntField(m_editorUtils.GetTextValue("TextureResolution"), m_profileValues.m_gradientTextureResolution), 1, 4096);
                    if (m_editorUtils.Button("GenerateTexture", GUILayout.Width(90f)))
                    {
                        string path = EditorUtility.SaveFilePanel("Save Water Depth Ramp as PNG", Application.dataPath, "New Water Depth Ramp.png", "png");
                        if (path.Length != 0)
                        {
                            GenerateGradientTexture(m_profileValues, path);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (m_profileValues.m_waterGradient == null)
                    {
                        m_profileValues.m_waterGradient = CreateNewWaterSurfaceGradient(0);
                    }
                }
                m_profileValues.m_transparentDistance = m_editorUtils.Slider("TransparentDistance", m_profileValues.m_transparentDistance, 0f, 32f, helpEnabled);
                m_profileValues.m_refractionRenderResolution = (GaiaConstants.PW_RENDER_SIZE)m_editorUtils.EnumPopup("RefractionResolution", m_profileValues.m_refractionRenderResolution, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Heading("UnderwaterSettings");
                EditorGUI.indentLevel++;
                if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    #if UNITY_POST_PROCESSING_STACK_V2
                    m_profileValues.PostProcessProfileBuiltIn = (PostProcessProfile)m_editorUtils.ObjectField("UnderwaterPostProcessingProfile", m_profileValues.PostProcessProfileBuiltIn, typeof(PostProcessProfile), false, helpEnabled);
                    #endif
                }
                else if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.Universal)
                {
#if UPPipeline
                    m_profileValues.PostProcessProfileURP = (VolumeProfile)m_editorUtils.ObjectField("UnderwaterPostProcessingProfile", m_profileValues.PostProcessProfileURP, typeof(VolumeProfile), false, helpEnabled);
#endif
                }
                else
                {
#if HDPipeline
                    m_profileValues.PostProcessProfileHDRP = (VolumeProfile)m_editorUtils.ObjectField("UnderwaterPostProcessingProfile", m_profileValues.PostProcessProfileHDRP, typeof(VolumeProfile), false, helpEnabled);
#endif
                }

                if (m_profileValues.m_underwaterFogGradient == null)
                {
                    m_profileValues.m_underwaterFogGradient = new Gradient();
                }
                m_profileValues.m_underwaterFogGradient = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("UnderwaterFog"), m_editorUtils.GetTooltip("UnderwaterFog")), m_profileValues.m_underwaterFogGradient);
                m_editorUtils.InlineHelp("UnderwaterFog", helpEnabled);
                if (RenderSettings.fogMode == FogMode.Exponential || RenderSettings.fogMode == FogMode.ExponentialSquared)
                {
                    m_profileValues.m_underwaterFogDensity = m_editorUtils.Slider("UnderwaterFogDensity", m_profileValues.m_underwaterFogDensity, 0f, 1f, helpEnabled);
                }
                else
                {
                    m_profileValues.m_underwaterNearFogDistance = m_editorUtils.FloatField("UnderwaterFogStart", m_profileValues.m_underwaterNearFogDistance, helpEnabled);
                    m_profileValues.m_underwaterFogDistance = m_editorUtils.FloatField("UnderwaterFogEnd", m_profileValues.m_underwaterFogDistance, helpEnabled);
                }
                m_editorUtils.Heading("UnderwaterPostFX");
#if GAIA_PRO_PRESENT
                if (ProceduralWorldsGlobalWeather.Instance == null)
                {
                    m_profileValues.m_constUnderwaterPostExposure = m_editorUtils.Slider("UnderwaterPostExposureFX", m_profileValues.m_constUnderwaterPostExposure, -5f, 5f, helpEnabled);
                    m_profileValues.m_constUnderwaterColorFilter = m_editorUtils.ColorField("UnderwaterColorFilterFX", m_profileValues.m_constUnderwaterColorFilter, helpEnabled);
                }
                else
                {
                    m_profileValues.m_gradientUnderwaterPostExposure = m_editorUtils.CurveField("UnderwaterPostExposureFX", m_profileValues.m_gradientUnderwaterPostExposure, helpEnabled);
                    m_profileValues.m_gradientUnderwaterColorFilter = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("UnderwaterColorFilterFX"), m_editorUtils.GetTooltip("UnderwaterColorFilterFX")), m_profileValues.m_gradientUnderwaterColorFilter);
                    m_editorUtils.InlineHelp("UnderwaterColorFilterFX", helpEnabled);
                }
#else
                m_profileValues.m_gradientUnderwaterPostExposure = m_editorUtils.CurveField("UnderwaterPostExposureFX", m_profileValues.m_gradientUnderwaterPostExposure, helpEnabled);
                m_profileValues.m_gradientUnderwaterColorFilter = EditorGUILayout.GradientField(new GUIContent(m_editorUtils.GetTextValue("UnderwaterColorFilterFX"), m_editorUtils.GetTooltip("UnderwaterColorFilterFX")), m_profileValues.m_gradientUnderwaterColorFilter);
                m_editorUtils.InlineHelp("UnderwaterColorFilterFX", helpEnabled);
#endif
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Heading("FadeSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_fadeStart = m_editorUtils.FloatField("FadeStart", m_profileValues.m_fadeStart, helpEnabled);
                m_profileValues.m_fadeDistance = m_editorUtils.FloatField("FadeDistance", m_profileValues.m_fadeDistance, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Heading("TilingSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_foamTiling = m_editorUtils.IntField("FoamTiling", m_profileValues.m_foamTiling, helpEnabled);
                m_profileValues.m_waterTiling = m_editorUtils.IntField("WaterTiling", m_profileValues.m_waterTiling, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Heading("NormalSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_normalLayer0 = (Texture2D)m_editorUtils.ObjectField("NormalLayer0", m_profileValues.m_normalLayer0, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_normalStrength0 = m_editorUtils.Slider("NormalStrength0", m_profileValues.m_normalStrength0, 0f, 3f, helpEnabled);
                m_profileValues.m_normalLayer1 = (Texture2D)m_editorUtils.ObjectField("NormalLayer1", m_profileValues.m_normalLayer1, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_normalStrength1 = m_editorUtils.Slider("NormalStrength1", m_profileValues.m_normalStrength1, 0f, 3f, helpEnabled);
                m_profileValues.m_fadeNormal = (Texture2D)m_editorUtils.ObjectField("FadeNormal", m_profileValues.m_fadeNormal, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_fadeNormalStrength = m_editorUtils.Slider("FadeNormalStrength", m_profileValues.m_fadeNormalStrength, 0f, 3f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Heading("FoamSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_foamTexture = (Texture2D)m_editorUtils.ObjectField("FoamTexture", m_profileValues.m_foamTexture, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                m_profileValues.m_foamDistance = m_editorUtils.Slider("FoamDistance", m_profileValues.m_foamDistance, 0f, 16f, helpEnabled);
                m_profileValues.m_foamStrength = m_editorUtils.Slider("FoamStrength", m_profileValues.m_foamStrength, 0f, 2f, helpEnabled);
                if (m_renderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                {
                    m_profileValues.m_foamBubbleTexture = (Texture2D)m_editorUtils.ObjectField("FoamBubbleTexture", m_profileValues.m_foamBubbleTexture, typeof(Texture2D), false, helpEnabled, GUILayout.Height(16f));
                    m_profileValues.m_foamBubbleScale = m_editorUtils.Slider("FoamBubbleScale", m_profileValues.m_foamBubbleScale, 0f, 1f, helpEnabled);
                    m_profileValues.m_foamBubbleTiling = m_editorUtils.IntField("FoamBubbleTiling", m_profileValues.m_foamBubbleTiling, helpEnabled);
                    m_profileValues.m_foamBubblesStrength = m_editorUtils.FloatField("FoamBubblesStrength", m_profileValues.m_foamBubblesStrength, helpEnabled);
                }
                m_profileValues.m_foamEdge = m_editorUtils.Slider("FoamEdge", m_profileValues.m_foamEdge, 0f, 1f, helpEnabled);
                m_profileValues.m_foamMoveSpeed = m_editorUtils.Slider("FoamMoveSpeed", m_profileValues.m_foamMoveSpeed, 0f, 1f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Heading("ReflectionSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_metallic = m_editorUtils.Slider("Metallic", m_profileValues.m_metallic, 0f, 1f, helpEnabled);
                m_profileValues.m_smoothness = m_editorUtils.Slider("Smoothness", m_profileValues.m_smoothness, 0f, 1f, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                m_editorUtils.Heading("WaveSettings");
                EditorGUI.indentLevel++;
                m_profileValues.m_shorelineMovement = m_editorUtils.Slider("ShorelineMovement", m_profileValues.m_shorelineMovement, 0f, 1f, helpEnabled);
                m_profileValues.m_waveCount = m_editorUtils.FloatField("WaveCount", m_profileValues.m_waveCount, helpEnabled);
                m_profileValues.m_waveSpeed = m_editorUtils.FloatField("WaveSpeed", m_profileValues.m_waveSpeed, helpEnabled);
                m_profileValues.m_waveSize = m_editorUtils.FloatField("WaveSize", m_profileValues.m_waveSize, helpEnabled);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            else
            {
                m_gaiaOceanMat = GaiaWater.GetGaiaOceanMaterial();
                EditorGUILayout.HelpBox("Gaia Ocean Material was not found. The system will keep trying to search for 'Gaia Ocean.mat'", MessageType.Warning);
            }

            GUI.enabled = true;
            if (m_profile.m_waterEditSettings)
            {
                if (m_editorUtils.Button("SaveToGaiaDefaultProfile"))
                {
                    if (m_sceneWater != null)
                    {
                        m_sceneWater.SaveToGaiaDefault(m_profileValues);
                        EditorGUIUtility.ExitGUI();
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (m_profileValues.m_userCustomProfile && !m_profile.m_waterEditSettings)
                {
                    updateSettingsOnly = true;
                }
            }
        }
        private void CausticSettingsEnabled(bool helpEnabled)
        {
            m_profile.m_useCastics = m_editorUtils.Toggle("UseCaustics", m_profile.m_useCastics, helpEnabled);
            if (m_profile.m_useCastics)
            {
                m_profile.m_mainCausticLight = (Light)m_editorUtils.ObjectField("MainCausticLight", m_profile.m_mainCausticLight, typeof(Light), true, helpEnabled, GUILayout.Height(16f));
                if (m_profile.m_mainCausticLight == null)
                {
                    m_profile.m_mainCausticLight = GaiaUtils.GetMainDirectionalLight();
                }
                m_profile.m_causticFramePerSecond = m_editorUtils.IntSlider("CausticFPS", m_profile.m_causticFramePerSecond, 15, 120, helpEnabled);
                m_profile.m_causticSize = m_editorUtils.Slider("CausticSize", m_profile.m_causticSize, 0.1f, 100f, helpEnabled);

                EditorGUILayout.HelpBox("Caustics setup is applied directly to the Directional Light using the cookie setup.", MessageType.Info);
            }
        }
        private void SaveAndLoad(bool helpEnabled)
        {
            bool canBeSaved = true;
            if (!m_profile.DefaultWaterSet)
            {
                EditorGUILayout.HelpBox("No water profile settings have been saved. Please use gaia default water profile to save them here.", MessageType.Warning);
                canBeSaved = false;
            }
            EditorGUILayout.BeginHorizontal();
            if (!canBeSaved)
            {
                GUI.enabled = false;
            }

            string path = "";
            if (m_editorUtils.Button("SaveToFile"))
            {
                string sceneName = EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("Unable to save file as scene has not been saved. Please save your scene then try again.");
                }
               
                path = EditorUtility.SaveFilePanelInProject("Save Location", sceneName + " Profile", "asset" , "Save new profile asset");
                GaiaSceneManagement.SaveFile(m_profile, path);
                EditorGUIUtility.ExitGUI();
            }

            GUI.enabled = true;

            if (m_editorUtils.Button("LoadFromFile"))
            {
                path = EditorUtility.OpenFilePanel("Load Profile", Application.dataPath + "/Assets", "asset");
                GaiaSceneManagement.LoadWater(path);
                EditorGUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }
        private void ThirdPartyPanel(bool helpEnabled)
        {
            if (m_profile.m_thirdPartyWaterObject != null)
            {
                EditorGUILayout.LabelField("Current Active Third Party System: " + m_profile.m_thirdPartyWaterObject.name, EditorStyles.boldLabel);
            }

            EditorGUILayout.BeginHorizontal();
            m_profile.m_thirdPartyWaterObject = (GameObject)m_editorUtils.ObjectField("ThirdPartySystem", m_profile.m_thirdPartyWaterObject, typeof(GameObject), true, helpEnabled);
            if (m_editorUtils.Button("EditThirdParty", GUILayout.MaxWidth(50f)))
            {
                if (m_profile.m_thirdPartyWaterObject == null)
                {
                    Debug.LogWarning("No third party game object has been set");
                }
                else
                {
                    Selection.activeObject = m_profile.m_thirdPartyWaterObject;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        /// <summary>
        /// Adds a new user profile
        /// </summary>
        private void AddNewCustomProfile()
        {
            GaiaWaterProfileValues selectdValues = m_profileValues;
            GaiaWaterProfileValues newProfile = new GaiaWaterProfileValues();
            GaiaUtils.CopyFields(selectdValues, newProfile);
            int count = m_profile.m_waterProfiles.Count + 1;
            newProfile.m_typeOfWater = "New User Profile " + count;
            newProfile.m_userCustomProfile = true;
#if UNITY_POST_PROCESSING_STACK_V2
            newProfile.PostProcessProfileBuiltIn = selectdValues.PostProcessProfileBuiltIn;
#endif
#if UPPipeline
            newProfile.PostProcessProfileURP = selectdValues.PostProcessProfileURP;
#endif
#if HDPipeline
            newProfile.PostProcessProfileHDRP = selectdValues.PostProcessProfileHDRP;
#endif
            m_profile.m_waterProfiles.Add(newProfile);

            m_profile.m_selectedWaterProfileValuesIndex = m_profile.m_waterProfiles.Count - 1;

            if (m_profile.m_selectedWaterProfileValuesIndex != -99)
            {
                GaiaWater.GetProfile(newProfileListIndex, m_gaiaOceanMat, m_profile, true, true);
            }

            EditorUtility.SetDirty(m_profile);
        }
        /// <summary>
        /// Handy layer mask interface
        /// </summary>
        /// <param name="label"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        private static LayerMask LayerMaskField(string label, EditorUtils editorUtils, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            label = editorUtils.GetContent(label).text;
            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;
            return layerMask;
        }
        /// <summary>
        /// Gets the string name of the layer and the count
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="layerCount"></param>
        private static void GetUnityLayers(out List<string> layers, out int layerCount)
        {
            layers = new List<string>();
            layers.Clear(); 
            layerCount = 0;
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                layers.Add(layerName);
                layerCount++;
            }
        }
        /// <summary>
        /// Generates the gradient texture then bakes it into a file
        /// </summary>
        /// <param name="m_waterProfileValues"></param>
        /// <param name="path"></param>
        private void GenerateGradientTexture(GaiaWaterProfileValues m_waterProfileValues, string path)
        {
            if (m_waterTexture == null || m_waterTexture.wrapMode != TextureWrapMode.Clamp)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            else if (m_waterProfileValues.m_gradientTextureResolution != m_waterTexture.width)
            {
                m_waterTexture = new Texture2D(m_waterProfileValues.m_gradientTextureResolution,
                    m_waterProfileValues.m_gradientTextureResolution) {wrapMode = TextureWrapMode.Clamp};
            }
            if (m_waterTexture != null)
            {
                for (int x = 0; x < m_waterProfileValues.m_gradientTextureResolution; x++)
                {
                    for (int y = 0; y < m_waterProfileValues.m_gradientTextureResolution; y++)
                    {
                        Color color = m_waterProfileValues.m_waterGradient.Evaluate((float)x / (float)m_waterProfileValues.m_gradientTextureResolution);
                        m_waterTexture.SetPixel(x, y, color);
                    }
                }
                m_waterTexture.Apply();

                GenerateTexture(m_waterTexture, path);
            }
        }
        /// <summary>
        /// Generates a texture based of a Unity Gradient
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="path"></param>
        private void GenerateTexture(Texture2D texture, string path)
        {
            path = path.Replace(Application.dataPath, "Assets");

            var pngData = texture.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
            }

            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            m_profileValues.m_colorDepthRamp = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            m_profileValues.m_colorDepthRamp.wrapMode = TextureWrapMode.Clamp;

            m_waterTexture = null;
        }
        /// <summary>
        /// Creates a default gradient for water surface coloring
        /// </summary>
        /// <returns></returns>
        private Gradient CreateNewWaterSurfaceGradient(int profileIdx)
        {
            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

            if (profileIdx == 1)
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("E9E1D3");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("D9E0D0");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("B2DFD1");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("9ADFD4");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("7DBCB2");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("7BBCB1");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("629B91");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("5D988E");
                colorKeys[7].time = 1f;
            }
            else if (profileIdx == 2)
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("C9E9DA");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("D4D4D4");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("A4A4A4");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("909090");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("808080");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("636363");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("295571");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("142E48");
                colorKeys[7].time = 1f;
            }
            else if (profileIdx == 3)
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("C9E9DA");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("D4D4D4");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("A4A4A4");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("909090");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("808080");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("636363");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("296171");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("143F48");
                colorKeys[7].time = 1f;
            }
            else
            {
                colorKeys[0].color = GaiaUtils.GetColorFromHTML("FCFDFC");
                colorKeys[0].time = 0f;

                colorKeys[1].color = GaiaUtils.GetColorFromHTML("B5CAB9");
                colorKeys[1].time = 0.15f;

                colorKeys[2].color = GaiaUtils.GetColorFromHTML("739A7B");
                colorKeys[2].time = 0.3f;

                colorKeys[3].color = GaiaUtils.GetColorFromHTML("2D614A");
                colorKeys[3].time = 0.45f;

                colorKeys[4].color = GaiaUtils.GetColorFromHTML("24554A");
                colorKeys[4].time = 0.6f;

                colorKeys[5].color = GaiaUtils.GetColorFromHTML("1B434A");
                colorKeys[5].time = 0.75f;

                colorKeys[6].color = GaiaUtils.GetColorFromHTML("1A3B4A");
                colorKeys[6].time = 0.9f;

                colorKeys[7].color = GaiaUtils.GetColorFromHTML("17314A");
                colorKeys[7].time = 1f;
            }

            alphaKeys[0].alpha = 1f;
            alphaKeys[0].time = 0f;

            alphaKeys[1].alpha = 1f;
            alphaKeys[1].time = 1f;

            gradient.SetKeys(colorKeys, alphaKeys);

            return gradient;
        }

        #endregion
    }
}