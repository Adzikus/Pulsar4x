﻿using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SensorSignatureAtbDB : IComponentDesignAttribute
    {

        public EMWaveForm PartWaveForm = new EMWaveForm(0, 0, 0);
        public double PartWaveFormMag;

        //[JsonProperty]
        //public double WavelengthAverage_nm;

        //[JsonProperty]
        //public double WavelengthMin_nm;

        //[JsonProperty]
        //public double WavelengthMax_nm;

        public SensorSignatureAtbDB() { }

        //public SensorSignatureAtbDB(double thermalSig, double electroMagneticSig) : this((int)thermalSig, (int)electroMagneticSig) { }


        public SensorSignatureAtbDB(double _PartWaveFormMag_w = 0, double _WavelengthAverage_nm = 0, double _WavelengthMin_nm = 0, double _WavelengthMax_nm = 0)
        {
            PartWaveForm = new EMWaveForm(_WavelengthAverage_nm, _WavelengthMin_nm, _WavelengthMax_nm);
            PartWaveFormMag = _PartWaveFormMag_w;
        }

        public SensorSignatureAtbDB(double _PartWaveFormMag_w, EMWaveForm _PartWaveForm)
        {
            PartWaveForm = _PartWaveForm;
            PartWaveFormMag = _PartWaveFormMag_w;
        }

        public SensorSignatureAtbDB(double temp_kelvin, double magnatude_watts)
        {
            double b = 2898000; //Wien's displacement constant for nanometers.
            var wavelength = b / temp_kelvin; //Wien's displacement law https://en.wikipedia.org/wiki/Wien%27s_displacement_law
            EMWaveForm waveform = new EMWaveForm(wavelength - 400, wavelength, wavelength + 600);
            PartWaveForm = waveform;
            PartWaveFormMag = magnatude_watts;
        }
        //public SensorSignatureAtbDB(double _WavelengthAverage_nm, double _WavelengthMin_nm, double _WavelengthMax_nm, double _PartWaveFormMag_w) : this((int)_WavelengthAverage_nm, (int)_WavelengthMin_nm, (int)_WavelengthMax_nm, (int)_PartWaveFormMag_w) { }

        public object Clone()
        {
            return new SensorSignatureAtbDB(PartWaveFormMag, PartWaveForm);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<SensorProfileDB>())
                parentEntity.SetDataBlob(new SensorProfileDB());

            if (PartWaveForm.WavelengthAverage_nm == 0)
            {
            }

            SensorProfileDB _PartSensorProfile = parentEntity.GetDataBlob<SensorProfileDB>();

            if(_PartSensorProfile.EmittedEMSpectra.ContainsKey(PartWaveForm)) 
            {
                _PartSensorProfile.EmittedEMSpectra[PartWaveForm] = _PartSensorProfile.EmittedEMSpectra[PartWaveForm] + PartWaveFormMag;
            }
            else
                _PartSensorProfile.EmittedEMSpectra.Add(PartWaveForm, PartWaveFormMag);

            parentEntity.SetDataBlob<SensorProfileDB>(_PartSensorProfile);
        }
        
        public string AtbName()
        {
            return "Sensor Signature";
        }

        public string AtbDescription()
        {

            return " ";
        }
    }
}