#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class MyRFLiveLong : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int period = 14;
		private int longTargetTicks = 80;
		private int shortTargetTicks = 80;
		private int longStopTicks = 60;
		private int shortStopTicks = 60;
		private int longExitBars = 24;
		private int shortExitBars = 24;
		private int maxEntries = 1;
        // User defined variables (add any user defined variables below)
		private int entries = 0;
		private bool longFlag = true;
		private bool shortFlag = true;
		private double highWater = 0.0;
		private double maxUnrealDD = 0.0;
		private double lTrailStop;
		private double sTrailStop;
        private IOrder myLongEntry 		= null;
		private IOrder myLongExit  		= null;
		private IOrder myShortEntry 	= null;
		private IOrder myShortExit  	= null;
		private IOrder myLongStop 		= null; // This variable holds an object representing our stop loss order
		private IOrder myLongTarget 	= null;
		private IOrder myShortStop 		= null; // This variable holds an object representing our stop loss order
		private IOrder myShortTarget 	= null;
		private IOrder myLongTimeExit	= null;
		private IOrder myShortTimeExit 	= null;


		#endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            Add(TSMA(OHLC4(Period),Period));
			//TSMA(OHLC4(Period),Period).Plots[0].Pen.Color = Color.Orange;  // set plot color
			TSMA(OHLC4(Period),Period).Plots[0].Pen.Width = 3;          // set line width
			//Add(VOL());
			
            CalculateOnBarClose = true;
			TraceOrders = false;
			
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			//RETURN IF NOT ENOUGH BARS FOR INDICATOR
			if (CurrentBar < Period) return;
			
			//PASS MARGIN AND UNREALIZED DD TO OPTIMIZER
			double MyValue2=Instrument.MasterInstrument.Margin;
			Variable2=MyValue2 * DefaultQuantity;
			
			
			if (Position.MarketPosition == MarketPosition.Flat)
				highWater = 0.0;

			if (Position.MarketPosition != MarketPosition.Flat)
        	{
				if (Position.GetProfitLoss(Close[0], PerformanceUnit.Currency) > highWater)
					highWater = Position.GetProfitLoss(Close[0], PerformanceUnit.Currency);
				if (Position.GetProfitLoss(Close[0], PerformanceUnit.Currency) - highWater < maxUnrealDD)
					maxUnrealDD = Position.GetProfitLoss(Close[0], PerformanceUnit.Currency) - highWater;
			}
			Variable3=maxUnrealDD;

			//Re-enable longs at session start
			if(Bars.FirstBarOfSession)
			{
				longFlag = true;
				shortFlag = true;
				entries = 0;
			}

			//PREVENT TRADES ON FIRST BAR
			if(ToTime(Time[0]) > 154450 && ToTime(Time[0]) < 160005)
			{
				longFlag = false;
				shortFlag = false;
			}
				
            // Condition set 1
            if (Rising(TSMA(OHLC4(Period),Period)))
			//if(TSMA(OHLC4(Period),Period)[0] > TSMA(OHLC4(Period),Period)[1] && TSMA(OHLC4(Period),Period)[1] > TSMA(OHLC4(Period),Period)[2])
			//if(TSMA(OHLC4(Period),Period)[0] > TSMA(OHLC4(Period),Period)[2])
            {
				if(myShortEntry != null)
				{
					CancelOrder(myShortEntry);
					myShortEntry = null;
				}
				if(myLongExit != null)
				{
					CancelOrder(myLongExit);
					myLongExit = null;
				}
				
				shortFlag = true;
				if(Position.MarketPosition == MarketPosition.Short)
					myShortExit = ExitShort(0, DefaultQuantity, "ShortExit1", "ShortEntry1");
				

				if(myLongEntry == null && myShortExit == null && Position.MarketPosition == MarketPosition.Flat && longFlag && entries < MaxEntries)
				{
					myLongEntry = EnterLong(0, DefaultQuantity, "LongEntry1");
				}

			}

            // Condition set 2
            if (Falling(TSMA(OHLC4(Period),Period)))
			//if(TSMA(OHLC4(Period),Period)[0] < TSMA(OHLC4(Period),Period)[1] && TSMA(OHLC4(Period),Period)[1] < TSMA(OHLC4(Period),Period)[2])
			//if(TSMA(OHLC4(Period),Period)[0] < TSMA(OHLC4(Period),Period)[2])
            {
				if(myLongEntry != null)
				{
					CancelOrder(myLongEntry);
					myLongEntry = null;
				}
				if(myShortExit != null)
				{
					CancelOrder(myShortExit);
					myShortExit = null;
				}
				longFlag = true;
				
				if(Position.MarketPosition == MarketPosition.Long)
					myLongExit = ExitLong(0, DefaultQuantity, "LongExit1", "LongEntry1");
				

				if(myShortEntry == null && myLongExit == null && Position.MarketPosition == MarketPosition.Flat && shortFlag && entries < MaxEntries)
				{
					//myShortEntry = EnterShort(0, DefaultQuantity, "ShortEntry1");
				}
				
				
			}
			
			
			//Stop, target and time exits with market orders
			if (Position.MarketPosition == MarketPosition.Long && myLongExit == null)
        	{
				//if (Position.GetProfitLoss(Close[0], PerformanceUnit.Currency) - highWater <= -1 * LongStopTicks * TickSize * Instrument.MasterInstrument.PointValue)
				/*
				if ((Close[0] - Close[BarsSinceEntry()+1]) * Instrument.MasterInstrument.PointValue - highWater <= -1 * LongStopTicks * TickSize * Instrument.MasterInstrument.PointValue)
				{
					myLongStop = ExitLong(0, DefaultQuantity, "LongStop1", "LongEntry1");
					highWater = 0.0;
					longFlag = false;
				}
				//if (Position.GetProfitLoss(Close[0], PerformanceUnit.Currency) >= LongTargetTicks * TickSize * Instrument.MasterInstrument.PointValue)
				if ((Close[0] - Close[BarsSinceEntry()+1]) * Instrument.MasterInstrument.PointValue >= LongTargetTicks * TickSize * Instrument.MasterInstrument.PointValue)
				{
					myLongTarget = ExitLong(0, DefaultQuantity, "LongTarget1", "LongEntry1");
					highWater = 0.0;
					longFlag = false;
				}
				*/
				//if (BarsSinceEntry()+1 >= LongExitBars && myLongTarget == null && myLongStop == null)
				if (BarsSinceEntry()+1 >= LongExitBars)
				{
					myLongTimeExit = ExitLong(0, DefaultQuantity, "LongTimeExit1", "LongEntry1");
					highWater = 0.0;
					longFlag = false;
				}
			}
			
			if (Position.MarketPosition == MarketPosition.Short && myShortExit == null)
        	{
				/*
				//if (Position.GetProfitLoss(Close[0], PerformanceUnit.Currency) - highWater <= -1 * ShortStopTicks * TickSize * Instrument.MasterInstrument.PointValue)
				if ((Close[BarsSinceEntry()+1] - Close[0]) * Instrument.MasterInstrument.PointValue - highWater <= -1 * ShortStopTicks * TickSize * Instrument.MasterInstrument.PointValue)
				{
					myShortStop = ExitShort(0, DefaultQuantity, "ShortStop1", "ShortEntry1");
					highWater = 0.0;
					shortFlag = false;
				}
				//if (Position.GetProfitLoss(Close[0], PerformanceUnit.Currency) >= ShortTargetTicks * TickSize * Instrument.MasterInstrument.PointValue)
				if ((Close[BarsSinceEntry()+1] - Close[0]) * Instrument.MasterInstrument.PointValue >= ShortTargetTicks * TickSize * Instrument.MasterInstrument.PointValue)
				{
					myShortTarget = ExitShort(0, DefaultQuantity, "ShortTarget1", "ShortEntry1");
					highWater = 0.0;
					shortFlag = false;
				}
				*/
				//if (BarsSinceEntry()+1 >= ShortExitBars && myShortTarget == null && myShortStop == null)
				/*
				if (BarsSinceEntry()+1 >= ShortExitBars)
				{
					myShortTimeExit = ExitShort(0, DefaultQuantity, "ShortTimeExit1", "ShortEntry1");
					highWater = 0.0;
					shortFlag = false;
				}
				*/
			}
		
			//TRAILING STOP
			
			if (Position.MarketPosition == MarketPosition.Long)
			{
				//myLongExit = ExitLongStop(0, true, DefaultQuantity, lTrailPrice, "LongExit1", "LongEntry1");

				if (High[0] - LongStopTicks * TickSize > lTrailStop)
				{
					lTrailStop = High[0] - LongStopTicks * TickSize;
					
					if(lTrailStop < Close[0])
						myLongStop = ExitLongStop(0, true, DefaultQuantity, lTrailStop, "LongStop1", "LongEntry1");
				}
			}
			/*
			if (Position.MarketPosition == MarketPosition.Short)
			{
				//myShortExit = ExitShortStop(0, true, DefaultQuantity, sTrailPrice, "ShortExit1", "ShortEntry1");
				
				if (Low[0] + ShortStopTicks * TickSize < sTrailStop)
				{
					sTrailStop = Low[0] +  ShortStopTicks * TickSize;
					
					if(sTrailStop > Close[0])
						myShortStop = ExitShortStop(0, true, DefaultQuantity, sTrailStop, "ShortStop1", "ShortEntry1");
				}
			}
			*/
			
        }
		
		protected override void OnOrderUpdate(IOrder order)
        {
			// Handle entry orders here. The entryOrder object allows us to identify that the order that is calling the OnOrderUpdate() method is the entry order.
			if (myLongEntry != null && myLongEntry == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myLongEntry = null;
				}
			}
			if (myShortEntry != null && myShortEntry == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myShortEntry = null;
				}
			}
			if (myLongExit != null && myLongExit == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myLongExit = null;
				}
			}
			if (myShortExit != null && myShortExit == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myShortExit = null;
				}
			}
			if (myLongStop != null && myLongStop == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myLongStop = null;
				}
			}
			if (myLongTarget != null && myLongTarget == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myLongTarget = null;
				}
			}
			if (myShortStop != null && myShortStop == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myShortStop = null;
				}
			}
			if (myShortTarget != null && myShortTarget == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myShortTarget = null;
				}
			}
			if (myLongTimeExit != null && myLongTimeExit == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myLongTimeExit = null;
				}
			}
			if (myShortTimeExit != null && myShortTimeExit == order)
			{	
				// Reset the entryOrder object to null if order was cancelled without any fill
				if (order.OrderState == OrderState.Cancelled && order.Filled == 0)
				{
					myShortTimeExit = null;
				}
			}
			if(order.OrderState == OrderState.PendingSubmit)
				if (order.Name == "Exit on close")
				{
					longFlag = false;
					shortFlag = false;
				}
        }
		
		protected override void OnExecution(IExecution execution)
        {
			if(execution.Name == "Exit on close")
			{
				longFlag = false;
				shortFlag = false;
			}
			/* We advise monitoring OnExecution to trigger submission of stop/target orders instead of OnOrderUpdate() since OnExecution() is called after OnOrderUpdate()
			which ensures your strategy has received the execution which is used for internal signal tracking. */
			if (myLongEntry != null && myLongEntry == execution.Order)
			{
				if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled || (execution.Order.OrderState == OrderState.Cancelled && execution.Order.Filled > 0))
				{
					// Stop-Loss order 4 ticks below our entry price
					lTrailStop = Math.Min(Close[0] - TickSize,Close[0] - LongStopTicks * TickSize);
					myLongStop = ExitLongStop(0, true, execution.Order.Filled, lTrailStop, "LongStop1", "LongEntry1");
					
					
					
					// Target order 8 ticks above our entry price
					myLongTarget = ExitLongLimit(0, true, execution.Order.Filled, Close[0] + LongTargetTicks * TickSize, "LongTarget1", "LongEntry1");
					
					// Resets the entryOrder object to null after the order has been filled
					if (execution.Order.OrderState != OrderState.PartFilled)
					{
						myLongEntry 	= null;
						longFlag = false;
						entries++;
					}
				}
			}
			
			// Reset our stop order and target orders' IOrder objects after our position is closed.
			if ((myLongStop != null && myLongStop == execution.Order) || (myLongTarget != null && myLongTarget == execution.Order) || (myLongTimeExit != null && myLongTimeExit == execution.Order))
			{
				if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
				{
					myLongStop		= null;
					myLongTarget	= null;
					myLongExit		= null;
					myLongTimeExit	= null;
					longFlag		= false;
				}
			}
			
			if (myShortEntry != null && myShortEntry == execution.Order)
			{
				if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled || (execution.Order.OrderState == OrderState.Cancelled && execution.Order.Filled > 0))
				{
					// Stop-Loss order 4 ticks below our entry price
					//sTrailStop = Math.Max(Close[0] + TickSize,Close[0] + ShortStopTicks * TickSize);
					//myShortStop = ExitShortStop(0, true, execution.Order.Filled, sTrailStop, "ShortStop1", "ShortEntry1");
					
					// Target order 8 ticks above our entry price
					//myShortTarget = ExitShortLimit(0, true, execution.Order.Filled, Close[0] - ShortTargetTicks * TickSize, "ShortTarget1", "ShortEntry1");
					
					// Resets the entryOrder object to null after the order has been filled
					if (execution.Order.OrderState != OrderState.PartFilled)
					{
						shortFlag = false;
						myShortEntry 	= null;
						entries++;
					}
				}
			}
			
			// Reset our stop order and target orders' IOrder objects after our position is closed.
			if ((myShortStop != null && myShortStop == execution.Order) || (myShortTarget != null && myShortTarget == execution.Order) || (myShortTimeExit != null && myShortTimeExit == execution.Order))
			{
				if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
				{
					myShortTarget 	= null;
					myShortStop   	= null;
					myShortExit 	= null;
					myShortTimeExit	= null;
					shortFlag 		= false;
				}
			}

			if (myLongExit != null && myLongExit == execution.Order)
			{
				if (execution.Order.OrderState == OrderState.Filled || (execution.Order.OrderState == OrderState.Cancelled && execution.Order.Filled > 0))
				{
					// Resets the entryOrder object to null after the order has been filled
					if (execution.Order.OrderState != OrderState.PartFilled)
					{
						myLongExit 		= null;
						myLongStop 		= null;
						myLongTimeExit	= null;
					}
					// Reverse position after exit fill
						highWater = 0.0;
					if(shortFlag && entries < MaxEntries)
					{
						//myShortEntry = EnterShort(0, DefaultQuantity, "ShortEntry1");
					}

				}
			}
			if (myShortExit != null && myShortExit == execution.Order)
			{
				if (execution.Order.OrderState == OrderState.Filled || (execution.Order.OrderState == OrderState.Cancelled && execution.Order.Filled > 0))
				{
					// Resets the entryOrder object to null after the order has been filled
					if (execution.Order.OrderState != OrderState.PartFilled)
					{
						myShortExit 	= null;
						myShortStop		= null;
						myShortTimeExit	= null;
					}
					// Reverse position after exit fill
						highWater = 0.0;
					if(longFlag && entries < MaxEntries)
						myLongEntry = EnterLong(0, DefaultQuantity, "LongEntry1");

				}
				
			}
		}

        #region Properties
		
        [Description("")]
        [GridCategory("Parameters")]
		[Gui.Design.DisplayName ("\t\t\t\tPeriod")]
        public int Period
        {
            get { return period; }
            set { period = Math.Max(1, value); }
        }
		
		[Description("")]
        [GridCategory("Parameters")]
		[Gui.Design.DisplayName ("\t\t\tLong Target Ticks")]
        public int LongTargetTicks
        {
            get { return longTargetTicks; }
            set { longTargetTicks = Math.Max(1, value); }
        }
		
		
		[Description("")]
        [GridCategory("Parameters")]
		[Gui.Design.DisplayName ("\t\tLong Stop Ticks")]
        public int LongStopTicks
        {
            get { return longStopTicks; }
            set { longStopTicks = Math.Max(1, value); }
        }
		
		
		[Description("")]
        [GridCategory("Parameters")]
		[Gui.Design.DisplayName ("\tLong Exit Bars")]
        public int LongExitBars
        {
            get { return longExitBars; }
            set { longExitBars = Math.Max(1, value); }
        }
		
		
		[Description("")]
        [GridCategory("Parameters")]
		[Gui.Design.DisplayName ("Max Entries")]
        public int MaxEntries
        {
            get { return maxEntries; }
            set { maxEntries = Math.Max(1, value); }
        }
		
        #endregion
    }
}
