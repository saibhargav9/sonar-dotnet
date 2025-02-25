/*
 * SonarVB
 * Copyright (C) 2012-2022 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */
package org.sonar.plugins.vbnet;

import org.junit.Test;
import org.sonar.api.rules.RuleType;
import org.sonar.api.server.debt.DebtRemediationFunction;
import org.sonar.api.server.rule.RulesDefinition;
import org.sonar.api.server.rule.RulesDefinition.Context;
import org.sonar.api.server.rule.RulesDefinition.Rule;

import static org.assertj.core.api.Assertions.assertThat;

public class VbNetSonarRulesDefinitionTest {
  private static final String SECURITY_HOTSPOT_RULE_KEY = "S4792";

  @Test
  public void test() {
    Context context = new Context();
    assertThat(context.repositories()).isEmpty();

    VbNetSonarRulesDefinition vbnetRulesDefinition = new VbNetSonarRulesDefinition();
    vbnetRulesDefinition.define(context);

    assertThat(context.repositories()).hasSize(1);
    assertThat(context.repository("vbnet").rules()).isNotEmpty();

    Rule s100 = context.repository("vbnet").rule("S1197");
    assertThat(s100.debtRemediationFunction().type()).isEqualTo(DebtRemediationFunction.Type.CONSTANT_ISSUE);
    assertThat(s100.debtRemediationFunction().baseEffort()).isEqualTo("5min");
    assertThat(s100.type()).isEqualTo(RuleType.CODE_SMELL);
  }

  @Test
  public void test_security_hotspot() {
    VbNetSonarRulesDefinition definition = new VbNetSonarRulesDefinition();
    RulesDefinition.Context context = new RulesDefinition.Context();
    definition.define(context);
    RulesDefinition.Repository repository = context.repository("vbnet");

    RulesDefinition.Rule hardcodedCredentialsRule = repository.rule(SECURITY_HOTSPOT_RULE_KEY);
    assertThat(hardcodedCredentialsRule.type()).isEqualTo(RuleType.SECURITY_HOTSPOT);
    assertThat(hardcodedCredentialsRule.activatedByDefault()).isFalse();
  }

  @Test
  public void test_security_hotspot_has_correct_type_and_security_standards() {
    VbNetSonarRulesDefinition definition = new VbNetSonarRulesDefinition();
    RulesDefinition.Context context = new RulesDefinition.Context();
    definition.define(context);
    RulesDefinition.Repository repository = context.repository("vbnet");

    RulesDefinition.Rule rule = repository.rule(SECURITY_HOTSPOT_RULE_KEY);
    assertThat(rule.type()).isEqualTo(RuleType.SECURITY_HOTSPOT);
    assertThat(rule.securityStandards()).containsExactlyInAnyOrder("cwe:117", "cwe:532", "owaspTop10:a10", "owaspTop10:a3");
  }

  @Test
  public void test_all_rules_have_status_set(){
    VbNetSonarRulesDefinition definition = new VbNetSonarRulesDefinition();
    RulesDefinition.Context context = new RulesDefinition.Context();
    definition.define(context);
    RulesDefinition.Repository repository = context.repository("vbnet");

    for (RulesDefinition.Rule rule:repository.rules()) {
      assertThat(rule.status()).isNotNull();
    }
  }
}
