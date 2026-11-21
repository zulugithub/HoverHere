"""Tests of fix_param_xml.py: py -3 -m unittest test_fix_param_xml (from schema/tools)."""
import unittest

from fix_param_xml import fix_text

BROKEN = (
    "<stru_transmitter_and_helicopter>\r\n"
    "  <helicopter>\r\n"
    "    <fuselage>\r\n"
    "      <S_fxyz>\r\n"
    "        <vect3>  <x>0.1</x>  <y>0.3</y>  <z>0.3</z>  </vect3>\r\n"
    "        <downwash_factor_mainrotor>  <val>0.8</val>  </downwash_factor_mainrotor>\r\n"
    "        <downwash_factor_tailrotor>  <val>0.0</val>  </downwash_factor_tailrotor>\r\n"
    "      </S_fxyz>\r\n"
    "    </fuselage>\r\n"
    "    <propeller><x>1</x></propeller>\r\n"
    "  </helicopter>\r\n"
    "  <tuning>\r\n"
    "\t<rotor_tuning>\r\n"
    "\t\t<propeller>\r\n"
    "\t\t\t<inflow_factor>  <val>1.0</val> </inflow_factor>\r\n"
    "\t\t</propeller>\r\n"
    "\t</rotor_tuning>\r\n"
    "  </tuning>\r\n"
    "</stru_transmitter_and_helicopter>\r\n"
)


class FixParamXml(unittest.TestCase):
    def test_downwash_factors_move_up_one_level_and_keep_their_text(self):
        fixed = fix_text(BROKEN)
        self.assertIn(
            "      </S_fxyz>\r\n"
            "      <downwash_factor_mainrotor>  <val>0.8</val>  </downwash_factor_mainrotor>\r\n"
            "      <downwash_factor_tailrotor>  <val>0.0</val>  </downwash_factor_tailrotor>\r\n"
            "    </fuselage>\r\n", fixed)
        self.assertIn("      <S_fxyz>\r\n        <vect3>  <x>0.1</x>  <y>0.3</y>  <z>0.3</z>  </vect3>\r\n      </S_fxyz>\r\n", fixed)

    def test_only_the_propeller_of_the_rotor_tuning_is_renamed(self):
        fixed = fix_text(BROKEN)
        self.assertIn("\t\t<propeller_tuning>\r\n", fixed)
        self.assertIn("\t\t</propeller_tuning>\r\n", fixed)
        self.assertIn("    <propeller><x>1</x></propeller>\r\n", fixed)  # helicopter/propeller is correct

    def test_idempotent_and_line_endings_preserved(self):
        fixed = fix_text(BROKEN)
        self.assertEqual(fix_text(fixed), fixed)
        self.assertNotIn("\n", fixed.replace("\r\n", ""))

    def test_correct_files_are_untouched(self):
        ok = "<fuselage>\n  <S_fxyz>\n    <vect3/>\n  </S_fxyz>\n  <downwash_factor_mainrotor><val>1</val></downwash_factor_mainrotor>\n</fuselage>\n"
        self.assertEqual(fix_text(ok), ok)


ROOT_LEVEL_FLAPPING = (
    '<?xml version="1.0"?>\r\n'
    "<stru_transmitter_and_helicopter>\r\n"
    "  <helicopter>\r\n"
    "    <flybarless>\r\n"
    "      <K_p>  <val>1</val>  </K_p>\r\n"
    "    </flybarless>  </helicopter>\r\n"
    "    <flapping>\r\n"
    "      <hub_stiffness_mr>  <val>250</val>  </hub_stiffness_mr>\r\n"
    "      <e>  <val>0.05</val>  </e> \r\n"
    "    </flapping>\r\n"
    "</stru_transmitter_and_helicopter>\r\n"
)

DUPLICATED_PROPELLER = (
    "<stru_transmitter_and_helicopter>\r\n"
    "  <helicopter>\r\n"
    "    <propeller>\r\n"
    "      <b>  <val>0</val>  </b>\r\n"
    "    </propeller>\r\n"
    "    <fuselage>\r\n"
    "      <S_fxyz><vect3/></S_fxyz>\r\n"
    "    </fuselage>\r\n"
    "    <propeller>\r\n"
    "      <!-- stale copy -->\r\n"
    "      <dirL><vect3/></dirL>\r\n"
    "    </propeller>\r\n"
    "  </helicopter>\r\n"
    "</stru_transmitter_and_helicopter>\r\n"
)


class FixStructure(unittest.TestCase):
    def test_root_level_flapping_moves_into_helicopter_and_the_closing_tag_gets_its_own_line(self):
        fixed = fix_text(ROOT_LEVEL_FLAPPING)
        self.assertEqual(
            fixed,
            '<?xml version="1.0"?>\r\n'
            "<stru_transmitter_and_helicopter>\r\n"
            "  <helicopter>\r\n"
            "    <flybarless>\r\n"
            "      <K_p>  <val>1</val>  </K_p>\r\n"
            "    </flybarless>\r\n"
            "    <flapping>\r\n"
            "      <hub_stiffness_mr>  <val>250</val>  </hub_stiffness_mr>\r\n"
            "      <e>  <val>0.05</val>  </e> \r\n"
            "    </flapping>\r\n"
            "  </helicopter>\r\n"
            "</stru_transmitter_and_helicopter>\r\n")
        self.assertEqual(fix_text(fixed), fixed)

    def test_root_level_flapping_before_a_closing_tag_on_its_own_line(self):
        text = "<stru_transmitter_and_helicopter>\n  <helicopter>\n    <mass_total><val>1</val></mass_total>\n  </helicopter>\n  <flapping>\n    <e><val>1</val></e>\n  </flapping>\n</stru_transmitter_and_helicopter>\n"
        self.assertEqual(
            fix_text(text),
            "<stru_transmitter_and_helicopter>\n  <helicopter>\n    <mass_total><val>1</val></mass_total>\n  <flapping>\n    <e><val>1</val></e>\n  </flapping>\n  </helicopter>\n</stru_transmitter_and_helicopter>\n")

    def test_repeated_children_of_the_helicopter_are_removed_but_the_first_one_stays(self):
        fixed = fix_text(DUPLICATED_PROPELLER)
        self.assertEqual(fixed.count("<propeller>"), 1)
        self.assertNotIn("dirL", fixed)
        self.assertIn("    <propeller>\r\n      <b>  <val>0</val>  </b>\r\n    </propeller>\r\n    <fuselage>", fixed)
        self.assertEqual(fix_text(fixed), fixed)

    def test_other_documents_are_not_restructured(self):
        scenery = "<stru_scenery>\n  <flapping><e/></flapping>\n  <flapping><e/></flapping>\n</stru_scenery>\n"
        self.assertEqual(fix_text(scenery), scenery)


if __name__ == "__main__":
    unittest.main()
