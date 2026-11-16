package hr.brendigo.clanarina

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import hr.brendigo.clanarina.data.Totals
import hr.brendigo.clanarina.ui.Brand

@Composable
internal fun DashboardOverview(
    totals: Totals, hasMembers: Boolean, openPage: (String) -> Unit
) {
    Column(
        Modifier.fillMaxWidth().verticalScroll(rememberScrollState()),
        verticalArrangement=Arrangement.spacedBy(12.dp)
    ) {
        val metrics=listOf(
            Triple("Članovi",totals.members.toString(),Brand.Blue),
            Triple("Zaduženo",euro(totals.chargedCents),Brand.Gold),
            Triple("Evidentirano",euro(totals.paidCents),Brand.Teal),
            Triple("Dugovanja",euro(totals.outstandingCents),Brand.Danger),
            Triple("Preplate",euro(totals.creditCents),Brand.Blue)
        )
        BoxWithConstraints(Modifier.fillMaxWidth()) {
            val columns=when {
                maxWidth < 320.dp -> 1
                maxWidth < 820.dp -> 2
                else -> 3
            }
            Column {
                metrics.chunked(columns).forEach { row ->
                    Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                        row.forEach { (label,value,color) ->
                            StatCard(label,value,color,Modifier.weight(1f))
                        }
                        repeat(columns-row.size) { Spacer(Modifier.weight(1f)) }
                    }
                }
            }
        }

        Text(
            "Evidentirane uplate nisu automatski bankovno potvrđene.",
            color=Brand.Muted,style=MaterialTheme.typography.bodySmall
        )
        Card(
            modifier=Modifier.fillMaxWidth(),
            shape=RoundedCornerShape(16.dp),
            border=BorderStroke(1.dp,Brand.Stroke),
            colors=CardDefaults.cardColors(containerColor=Color.White)
        ) {
            Column(
                Modifier.padding(16.dp),
                verticalArrangement=Arrangement.spacedBy(10.dp)
            ) {
                Text("Brze radnje",fontWeight=FontWeight.Bold,color=Brand.Navy,
                    style=MaterialTheme.typography.titleMedium)
                BoxWithConstraints(Modifier.fillMaxWidth()) {
                    if (maxWidth < 400.dp) {
                        Column(verticalArrangement=Arrangement.spacedBy(8.dp)) {
                            Button(onClick={ openPage("Članovi") },modifier=Modifier.fillMaxWidth()) {
                                Text("Članovi")
                            }
                            OutlinedButton(
                                onClick={ openPage("Članarine") },
                                modifier=Modifier.fillMaxWidth(),
                                enabled=hasMembers
                            ) { Text("Članarine") }
                            OutlinedButton(
                                onClick={ openPage("Uplate") },
                                modifier=Modifier.fillMaxWidth(),
                                enabled=hasMembers
                            ) { Text("Uplate") }
                        }
                    } else {
                        Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                            Button(onClick={ openPage("Članovi") }) { Text("Članovi") }
                            OutlinedButton(
                                onClick={ openPage("Članarine") },enabled=hasMembers
                            ) { Text("Članarine") }
                            OutlinedButton(
                                onClick={ openPage("Uplate") },enabled=hasMembers
                            ) { Text("Uplate") }
                        }
                    }
                }
            }
        }
        Text("Podaci ostaju na ovom uređaju.",color=Brand.Muted,
            style=MaterialTheme.typography.bodySmall)
        Spacer(Modifier.height(8.dp))
    }
}
