using gip.core.autocomponent;
using gip.core.datamodel;
using gip.core.processapplication;
using System.Linq;

namespace gipbakery.mes.processapplication
{
    [ACClassInfo(Const.PackName_VarioSystem, "en{'BakeryConverterBaking'}de{'BakeryConverterBaking'}", Global.ACKinds.TPABGModule, Global.ACStorableTypes.NotStorable, false, false)]
    public class BakeryConverterBaking : PAFuncStateConvBase
    {

        #region ctor's

        public BakeryConverterBaking(ACClass acType, IACObject content, IACObject parentACObject, ACValueList parameter, string acIdentifier = "")
            : base(acType, content, parentACObject, parameter, acIdentifier)
        {
        }

        #endregion

        #region PAFuncStateConvBase abstract
        public override bool IsReadyForSending
        {
            get { return true; }
        }

        public override bool IsReadyForReading
        {
            get { return true; }
        }

        public override ACStateEnum GetNextACState(PAProcessFunction sender, string transitionMethod = "")
        {
            return sender.ACState.ValueT;
        }

        public override bool IsEnabledTransition(PAProcessFunction sender, string transitionMethod)
        {
            throw new System.NotImplementedException();
        }

        public override PAProcessFunction.CompleteResult ReceiveACMethodResult(PAProcessFunction sender, ACMethod acMethod, out MsgWithDetails msg)
        {
            msg = new MsgWithDetails();
            PAProcessFunction.CompleteResult completeResult = PAProcessFunction.CompleteResult.Succeeded;
            PAFBakeryWorkBaking pAFBakeryWorkBaking = sender as PAFBakeryWorkBaking;
            if (pAFBakeryWorkBaking != null)
            {
                PAProcessModule parentModule = pAFBakeryWorkBaking.FindParentComponent<PAProcessModule>();

                if (parentModule != null)
                {
                    PAEThermometer thermometer = parentModule.FindChildComponents<PAEThermometer>().FirstOrDefault();
                    if (thermometer != null)
                    {
                        acMethod.ResultValueList.GetACValue("Temperature").Value = thermometer.ActualValue.ValueT;
                    }
                    else
                    {
                        Msg errorThermometerNotFound = new Msg(eMsgLevel.Error, GetACUrl() + " - No Thermometer sensor found!");
                        msg.AddDetailMessage(errorThermometerNotFound);
                    }
                }
                else
                {
                    Msg errorNoProcessModule = new Msg(eMsgLevel.Error, GetACUrl() + " - No parent module found!");
                    msg.AddDetailMessage(errorNoProcessModule);
                }
            }
            return completeResult;
        }

        public override MsgWithDetails SendACMethod(PAProcessFunction sender, ACMethod acMethod, ACMethod previousParams = null)
        {
            MsgWithDetails msgWithDetails = null;
            //throw new System.NotImplementedException();
            return msgWithDetails;
        }

        protected override void ModelProperty_ValueUpdatedOnReceival(object sender, ACPropertyChangedEventArgs e, ACPropertyChangedPhase phase)
        {
            //throw new System.NotImplementedException();
        }

        #endregion


    }
}
