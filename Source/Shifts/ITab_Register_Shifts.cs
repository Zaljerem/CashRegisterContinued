using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using TimetableUtility = CashRegister.Timetable.TimetableUtility;

namespace CashRegister.Shifts
{
	public class ITab_Register_Shifts : ITab_Register
	{
		private const int MinDialogHeight = 240;
		private float lastHeight;

		public ITab_Register_Shifts() : base(new Vector2(1100, MinDialogHeight))
		{
			labelKey = "TabRegisterShifts";
		}

		public override bool IsVisible => true;

        private Vector2 scrollPosition = Vector2.zero;

        public override void FillTab()
        {
            const int WidthLabel = 300;
            const int MinHeight = 30;
            const float ScrollMargin = 10f;

            var outRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(ScrollMargin);
            var viewRect = outRect;
            float contentHeight = 0f;

            // Calculate the height dynamically
            viewRect.height = 10000f; // Large enough for any number of entries; gets clipped by outRect

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            var rect = new Rect(0f, 0f, viewRect.width, viewRect.height);
            var rectHeader = new Rect(rect) { height = Text.LineHeight };
            rect.yMin += Text.LineHeight;

            var rectAdd = new Rect(rect) { height = MinHeight, width = MinHeight };
            var rectLabel = new Rect(rect) { width = WidthLabel };
            var rectTable = new Rect(rect);
            rectLabel.x = rectAdd.xMax;
            rectTable.xMin = rectLabel.xMax;
            rectHeader.width = rectTable.width;

            // Draw header
            TimetableUtility.DoHeader(rectHeader);

            float currentY = rect.yMin;

            for (int i = 0; i < Register.shifts.Count; i++)
            {
                var shift = Register.shifts[i];
                float height = MinHeight;

                var labelRect = new Rect(rectLabel) { y = currentY };
                var tableRect = new Rect(rectTable) { y = currentY };
                var removeRect = new Rect(rectAdd) { y = currentY };

                if (shift != null)
                {
                    DrawShift(tableRect, labelRect, shift, ref height);
                }

                if (Widgets.ButtonText(removeRect, "TabRegisterShiftsRemove".Translate()))
                {
                    Register.shifts.RemoveAt(i);
                    break; // List modified — exit
                }

                currentY += height;
                contentHeight += height;
            }

            if (Register.shifts.Count < 50) // Arbitrary cap, or remove entirely
            {
                var addRect = new Rect(rectAdd) { y = currentY };
                DrawAddButton(addRect);
                contentHeight += MinHeight;
            }

            Widgets.EndScrollView();

            size.y = Mathf.Max(contentHeight + ScrollMargin * 2, MinDialogHeight);
        }


        private void DrawShift(Rect rectTable, Rect rectLabel, Shift shift, ref float height)
		{
			var names = shift.assigned.Any() ? shift.assigned.Select(pawn => GetPawnName(shift, pawn)).ToCommaList() : (string)"TabRegisterShiftsEmpty".Translate();
			var rectNames = new Rect(rectLabel) {width = rectLabel.width * 0.6f};
			var rectAssign = new Rect(rectLabel) {xMin = rectNames.xMax, height = height};
			DrawLabel(rectNames, names, out var labelHeight);

			if (Widgets.ButtonText(rectAssign, "TabRegisterShiftsAssign".Translate()))
			{
				Register.CompAssignableToPawn.SetAssigned(shift.assigned);
				Find.WindowStack.Add(new Dialog_AssignBuildingOwner(Register.CompAssignableToPawn));
			}
			TimetableUtility.DoCell(new Rect(rectTable) {height = height}, shift.timetable, Register.Map);
			height = Mathf.Max(height, labelHeight);
		}

        private string GetPawnName(Shift shift, Pawn pawn)
        {
            if (pawn == null) return null;
            if (!shift.IsActive) return pawn.Name.ToStringShort.Colorize(TimeAssignmentDefOf.Sleep.color * 1.3f);
            if (!Register.IsAvailable(pawn)) return pawn.Name.ToStringShort.Colorize(Color.gray);
            if (Register.HasToWork(pawn)) return pawn.Name.ToStringShort.Colorize(TimeAssignmentDefOf.Work.color * 1.3f);
            return pawn.Name.ToStringShort;
        }

        private static void DrawLabel(Rect rectLabel, string names, out float height)
		{
			Text.Font = GameFont.Tiny;
			rectLabel.height = height = Text.CalcHeight(names, rectLabel.width);
			//Widgets.DrawBox(rectLabel, 1);

			Widgets.Label(rectLabel, names);
			
			Text.Font = GameFont.Small;
		}

		private void DrawAddButton(Rect rectAdd)
		{
			if (Widgets.ButtonText(rectAdd, "TabRegisterShiftsAdd".Translate()))
			{
				Register.shifts.Add(new Shift {map = Register.Map});
			}
		}

		public override bool CanAssignToShift(Pawn pawn) => false;

		public override IEnumerable<Gizmo> GetGizmos()
		{
			if (Register?.Faction == Faction.OfPlayer)
			{
				var toggle = new Command_Toggle
				{
					hotKey = KeyBindingDefOf.Command_TogglePower,
					defaultLabel = "TabRegisterShiftsStandby".Translate(),
					defaultDesc = "TabRegisterShiftsStandbyDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/StandByJob"),
					isActive = () => Register.standby,
					toggleAction = () => Register.standby = !Register.standby
				};
				yield return toggle;
			}
		}
	}
}
