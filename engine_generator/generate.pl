#!/usr/bin/perl

use strict;
use warnings;

use lib qw(lib);
use List::Util qw[min max];
use POSIX;
use Mustache::Simple;

my $table_file = 'engine_tables.txt';

my $tache = new Mustache::Simple(
	throw => 1
);

open my $handle, '<', "icons.txt";
chomp(my @icons = <$handle>);
close $handle;

my %stockratings;
{
	open my $handle, '<', "stock_std_ratings.txt";
	chomp(my @ratings = <$handle>);
	close $handle;
	@stockratings{@ratings} = ();
}
{
	open my $handle, '<', "lore_ratings.txt";
	chomp(my @ratings = <$handle>);
	close $handle;
	@stockratings{@ratings} = ();
}

my $icon = "uixSvgIcon_equipment_Heatsink";
# useful to browse icons
sub next_icon {
	#push(@icons, shift(@icons));
	#$icon = $icons[0];
	return $icon;
}

open my $info, $table_file or die "Could not open $table_file: $!";

my $categories = {
	"basic" => [],
	"exotics" => []
};

my $header = <$info>;
while (my $line = <$info>)  {
	my @cols = split(' ', $line);
	my $rating = $cols[0];

	my $category = "basic";
	
	next unless (exists $stockratings{$rating});

	my $rating_string = sprintf('%03s', $rating);
	print($rating_string, " ");
	my $gyro_tons = int($rating / 100 + 0.5);
	my $gyro_cost = 300000 * $gyro_tons;
	my $heat_dissipation = min(floor($rating / 25), 10) * 3;
	my $additional_slots = max(floor($rating / 25 - 10), 0);

	my $generate_engine_sub = sub {
		my $prefix = shift;
		my $engine_tonnage = shift;
		my $engine_cost_per_rating = shift;
		
		my $engine_cost = $engine_cost_per_rating * $rating; # we assume 75 ton mech
		$engine_cost = $engine_cost / 2; # this is only the cost of the core, shielding is an extra component
		
		my $engine = {
			ID => "${prefix}_${rating_string}",
			RATING => $rating_string,
			TONNAGE => $engine_tonnage + $gyro_tons,
			COST => $engine_cost + $gyro_cost,
			ICON => next_icon(),
			BONUSA => "- ${heat_dissipation} Heat / Turn",
			BONUSB => $additional_slots == 0 ? " " : "+ $additional_slots Slots"
		};

		my $json = $tache->render("${prefix}_template.json", $engine);

		write_to_file("../data/$category/engines/$engine->{ID}.json", $json);
		my $engines = $categories->{$category};
		push(@$engines, $engine);
	};
	
	$generate_engine_sub->("emod_engine", $cols[5], 5000);
}

close $info;

while ((my $category, my $engines) = each(%{$categories})) {
	my $shop = {
		ID => "shopdef_emod_engines_${category}_test_generated",
		ENGINES => $engines
	};

	my $json = $tache->render('shopdef_emod_engines_template.json', $shop);
	write_to_file("../data/${category}/shops_test/$shop->{ID}.json", $json);
}

sub write_to_file {
	my $filename = shift;
	my $content = shift;
	open(my $fh, '>', $filename) or die "Could not open file '$filename' $!";
	print {$fh} $content;
	close $fh;
}
