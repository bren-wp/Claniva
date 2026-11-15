package hr.brendigo.clanarina

import androidx.compose.foundation.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.res.painterResource
import hr.brendigo.clanarina.data.LocalStore
import hr.brendigo.clanarina.ui.Brand
import java.math.BigDecimal
import java.text.NumberFormat
import java.util.Locale

private val sections = listOf("Početna","Članovi","Članarine","Uplate","Dugovanja","Uplatnice","Bankovni izvodi","Izvještaji","Licenca","Postavke")
private val euros = NumberFormat.getCurrencyInstance(Locale.forLanguageTag("hr-HR"))
internal fun euro(cents:Long) = euros.format(BigDecimal.valueOf(cents,2))

@OptIn(ExperimentalMaterial3Api::class)
@Composable private fun MainApp(db:LocalStore) {
    var page by remember { mutableStateOf("Početna") }
    var menu by remember { mutableStateOf(false) }
    var changes by remember { mutableIntStateOf(0) }
    val members=remember(changes) { db.listMembers() }
    val totals=remember(changes) { db.totals() }
    Scaffold(
        topBar={ TopAppBar(
            title={
                Row(verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.spacedBy(10.dp)) {
                    Image(painter=painterResource(id=R.drawable.brand_foreground),contentDescription="Logotip Članarina",
                        modifier=Modifier.size(35.dp))
                    Text("Članarina",fontWeight=FontWeight.Bold)
                }
            },
            actions={
                Box {
                    TextButton(onClick={menu=true}) { Text("Moduli",color=Color.White) }
                    DropdownMenu(expanded=menu,onDismissRequest={menu=false}) {
                        sections.forEach { name -> DropdownMenuItem(text={Text(name)},onClick={page=name;menu=false}) }
                    }
                }
            },
            colors=TopAppBarDefaults.topAppBarColors(containerColor=Brand.Navy,titleContentColor=Color.White)
        )},
        bottomBar={ NavigationBar {
            listOf("Početna","Članovi","Uplate","Više").forEach { tab ->
                NavigationBarItem(
                    selected=(page==tab || (tab=="Više" && page !in listOf("Početna","Članovi","Uplate"))),
                    onClick={page=if(tab=="Više") "Članarine" else tab},
                    icon={Text(when(tab){"Početna"->"⌂";"Članovi"->"●";"Uplate"->"✓";else->"⋯"})},
                    label={Text(tab)}
                )
            }
        }}
    ) { inner ->
        Column(Modifier.fillMaxSize().padding(inner).padding(horizontal=12.dp)) {
            Spacer(Modifier.height(16.dp))
            Text(page,style=MaterialTheme.typography.headlineMedium,fontWeight=FontWeight.Bold)
            Spacer(Modifier.height(14.dp))
            when(page) {
                "Početna" -> Column(Modifier.verticalScroll(rememberScrollState())) {
                    // Compact viewports and large fonts must not clip the balances.
                    BoxWithConstraints(Modifier.fillMaxWidth()) {
                        if (maxWidth < 410.dp) {
                            Column {
                                StatCard("Članovi", totals.members.toString(), Brand.Blue, Modifier.fillMaxWidth())
                                StatCard("Uplaćeno", euro(totals.paidCents), Brand.Teal, Modifier.fillMaxWidth())
                                StatCard("Zaduženo", euro(totals.chargedCents), Brand.Gold, Modifier.fillMaxWidth())
                                StatCard("Dugovanja", euro(totals.outstandingCents), Brand.Danger, Modifier.fillMaxWidth())
                            }
                        } else {
                            Column {
                                Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                                    StatCard("Članovi", totals.members.toString(), Brand.Blue, Modifier.weight(1f))
                                    StatCard("Uplaćeno", euro(totals.paidCents), Brand.Teal, Modifier.weight(1f))
                                }
                                Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                                    StatCard("Zaduženo", euro(totals.chargedCents), Brand.Gold, Modifier.weight(1f))
                                    StatCard("Dugovanja", euro(totals.outstandingCents), Brand.Danger, Modifier.weight(1f))
                                }
                            }
                        }
                    }
                    Spacer(Modifier.height(16.dp))
                    Text("Brze radnje",fontWeight=FontWeight.Bold)
                    Button(onClick={page="Članovi"}) { Text("Članovi / Novi član") }
                    OutlinedButton(onClick={page="Uplate"},enabled=members.isNotEmpty()) { Text("Nova uplata") }
                    Text("Podaci se čuvaju samo na ovom uređaju.",style=MaterialTheme.typography.bodySmall)
                }
                "Članovi","Dugovanja" -> MemberManagement(db, page=="Dugovanja", changes) { changes++ }
                "Članarine","Uplate" -> LedgerManagement(
                    db, if(page=="Članarine") "charge" else "payment", changes
                ) { changes++ }
                "Izvještaji" -> Column(Modifier.verticalScroll(rememberScrollState())) {
                    StatCard("Broj članova",totals.members.toString(),Brand.Blue)
                    StatCard("Zaduženja",euro(totals.chargedCents),Brand.Gold)
                    StatCard("Evidentirane uplate",euro(totals.paidCents),Brand.Teal)
                    StatCard("Nepodmireno",euro(totals.outstandingCents),Brand.Danger)
                    StatCard("Preplate",euro(totals.creditCents),Brand.Blue)
                }
                "Licenca" -> Empty("Online i offline aktivacija licence još nije povezana s aplikacijom.")
                "Uplatnice" -> Empty("HUB-3 i EPC QR još nisu implementirani ni verificirani.")
                "Bankovni izvodi" -> Empty("Uvoz i usklađivanje bankovnih izvoda još nisu implementirani.")
                "Postavke" -> Empty("Lokalna SQLite pohrana. Bez računa, clouda i praćenja korisnika.")
            }
        }
    }
}
