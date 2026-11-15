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
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.sp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.res.painterResource
import hr.brendigo.clanarina.data.LocalStore
import hr.brendigo.clanarina.ui.Brand
import java.math.BigDecimal
import java.text.NumberFormat
import java.util.Currency
import java.util.Locale

private val sections = listOf("Početna","Članovi","Članarine","Uplate","Dugovanja","Uplatnice","Bankovni izvodi","Izvještaji","Licenca","Postavke")
private val euros = NumberFormat.getCurrencyInstance(Locale.forLanguageTag("hr-HR")).apply {
    currency = Currency.getInstance("EUR")
}
internal fun euro(cents:Long) = euros.format(BigDecimal.valueOf(cents,2))

@OptIn(ExperimentalMaterial3Api::class)
@Composable internal fun MainApp(db:LocalStore) {
    var page by remember { mutableStateOf("Početna") }
    var menu by remember { mutableStateOf(false) }
    var moreMenu by remember { mutableStateOf(false) }
    var changes by remember { mutableIntStateOf(0) }
    val members=remember(changes) { db.listMembers() }
    val totals=remember(changes) { db.totals() }
    Scaffold(
        topBar={ TopAppBar(
            title={
                Row(
                    modifier=Modifier.fillMaxWidth(),
                    verticalAlignment=Alignment.CenterVertically,
                    horizontalArrangement=Arrangement.spacedBy(8.dp)
                ) {
                    Image(
                        painter=painterResource(id=R.drawable.brand_foreground),
                        contentDescription="Logotip Članarina",
                        modifier=Modifier.size(29.dp)
                    )
                    Text("Članarina",fontWeight=FontWeight.Bold,
                        maxLines=1,overflow=TextOverflow.Ellipsis,fontSize=20.sp)
                }
            },
            actions={
                Box {
                    IconButton(onClick={menu=true}) {
                        Text("⋮",color=Color.White,fontSize=24.sp)
                    }
                    DropdownMenu(expanded=menu,onDismissRequest={menu=false}) {
                        sections.forEach { name -> DropdownMenuItem(text={Text(name)},onClick={page=name;menu=false}) }
                    }
                }
            },
            colors=TopAppBarDefaults.topAppBarColors(containerColor=Brand.Navy,titleContentColor=Color.White)
        )},
        containerColor=Brand.Background,
        bottomBar={
            BoxWithConstraints(Modifier.fillMaxWidth()) {
                val compactLabels = maxWidth < 280.dp
                NavigationBar(containerColor=Color.White) {
                    listOf("Početna","Članovi","Uplate","Više").forEach { tab ->
                        NavigationBarItem(
                            selected=(page==tab ||
                                (tab=="Više" && page !in listOf("Početna","Članovi","Uplate"))),
                            onClick={if(tab=="Više") moreMenu=true else page=tab},
                            icon={Text(when(tab){
                                "Početna"->"⌂";"Članovi"->"○";"Uplate"->"✓";else->"⋯"
                            },color=Brand.Navy)},
                            label=if(compactLabels) null else ({
                                Text(tab,maxLines=1,overflow=TextOverflow.Ellipsis)
                            }),
                            alwaysShowLabel=!compactLabels
                        )
                    }
                }
            }
        }
    ) { inner ->
        Column(Modifier.fillMaxSize().padding(inner).padding(horizontal=12.dp)) {
            Spacer(Modifier.height(16.dp))
            Text(page,style=MaterialTheme.typography.headlineMedium,fontWeight=FontWeight.Bold)
            Spacer(Modifier.height(14.dp))
            when(page) {
                "Početna" -> DashboardOverview(totals, members.isNotEmpty()) { page = it }
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
    if (moreMenu) {
        ModalBottomSheet(onDismissRequest={moreMenu=false}) {
            Text("Svi moduli", style=MaterialTheme.typography.titleLarge,
                modifier=Modifier.padding(horizontal=20.dp, vertical=10.dp), color=Brand.Navy)
            sections.filter { it !in listOf("Početna","Članovi","Uplate") }.forEach { name ->
                TextButton(onClick={page=name;moreMenu=false},
                    modifier=Modifier.fillMaxWidth().heightIn(min=48.dp)) {
                    Text(name,modifier=Modifier.fillMaxWidth().padding(start=16.dp),color=Brand.Navy)
                }
            }
            Spacer(Modifier.height(26.dp))
        }
    }

}
